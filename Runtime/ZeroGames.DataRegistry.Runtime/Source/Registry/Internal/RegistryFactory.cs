// Copyright Zero Games. All Rights Reserved.

using System.Reflection;
using System.Xml.Linq;

namespace ZeroGames.DataRegistry.Runtime;

public class RegistryFactory : IRegistryFactory
{
	
	public T Create<T>(IXDocumentProvider sourceProvider, IEnumerable<IRegistry> imports) where T : class, IRegistry
	{
		XDocument document = sourceProvider.Document;
		Dictionary<string, IRegistry> importMap = imports.ToDictionary(import => import.Name);

		var registry = Activator.CreateInstance<T>();
		(registry as INotifyInitialization)?.PreInitialize();

		GetRegistryMetadata(typeof(T), out var metadata);
		
		Dictionary<Type, IRepository> repositoryByEntityType = [];
		{ // Stage I: Fill import registries
			foreach (var importProperty in metadata.Imports)
			{
				Type propertyType = importProperty.PropertyType;
				string name = propertyType.Name;
				if (!importMap.TryGetValue(name, out var import))
				{
					throw new KeyNotFoundException($"Import registry '{name}' not found.");
				}

				Type importType = import.GetType();
				if (!importType.IsAssignableTo(propertyType))
				{
					throw new ArgumentException($"Import registry '{name}' is not assignable to property with type '{propertyType}'.");
				}
			
				importProperty.SetValue(registry, import);
				
				GetRegistryMetadata(importType, out var importMetadata);
				foreach (var repositoryProperty in importMetadata.Repositories)
				{
					Type repositoryType = repositoryProperty.PropertyType;
					Type entityType = repositoryType.GetGenericInstanceOf(typeof(IRepository<,>))!.GetGenericArguments()[1];
					repositoryByEntityType[entityType] = (IRepository)repositoryProperty.GetValue(import)!;
				}
			}
		}
		
		RepositoryFactory factory = new()
		{
			PrimitiveSerializerMap = new Dictionary<Type, Func<string, object>>(),
		};
		var finishInitializations = new RepositoryFactory.FinishInitializationDelegate[metadata.Repositories.Count];
		{ // Stage II: Allocate all repositories first but not initialize here (only properties defined by IEntity interface is available on entities).
			int32 i = 0;
			foreach (var repositoryProperty in metadata.Repositories)
			{
				Type propertyType = repositoryProperty.PropertyType;
				Type entityType = propertyType.GetGenericInstanceOf(typeof(IRepository<,>))!.GetGenericArguments()[1];
				XElement? root = document.Root?.Element($"{entityType.Name}Repository");
				if (root is null ^ entityType.IsAbstract)
				{
					throw new InvalidOperationException();
				}

				IInitializingRepository repository = root is not null ? factory.Create(registry, entityType, root, out var finishInitialization) : factory.Create(registry, entityType, out finishInitialization);
				if (!repository.GetType().IsAssignableTo(propertyType))
				{
					throw new InvalidOperationException();
				}
			
				repositoryProperty.SetValue(registry, repository);

				finishInitializations[i++] = finishInitialization;
				repositoryByEntityType[entityType] = repository;
			}
		}
		
		{ // Stage III: Merge concrete entities in inherited repository into base repository.
			foreach (var repository in repositoryByEntityType
				         .Select(pair => pair.Value)
				         .Where(repo => repo.EntityType.BaseType is {} baseType && baseType != typeof(object))
				         .OrderByDescending(repo =>
				         {
					         int32 depth = 0;
					         for (Type? baseType = repo.EntityType.BaseType; baseType is {} b && b != typeof(object); baseType = baseType.BaseType) ++depth;
					         return depth;
				         }))
			{
				var baseRepository = (IInitializingRepository)repositoryByEntityType[repository.EntityType.BaseType!];
				foreach (var entity in repository.Entities)
				{
					baseRepository.RegisterEntity(entity, false);
				}
			}
		}
		
		{ // Stage IV: Now all entities are in right place, and we can initialize them: Fill data, fixup references, etc.
			RepositoryFactory.GetEntityDelegate getEntity = (type, primaryKey) => repositoryByEntityType[type][primaryKey];
			foreach (var finishInitialization in finishInitializations)
			{
				finishInitialization(getEntity);
			}
		}

		{ // Stage V: Build indices.
			foreach (var autoIndex in metadata.AutoIndices)
			{
				throw new NotImplementedException();
			}
		}
		
		(registry as INotifyInitialization)?.PostInitialize();
		return registry;
	}

	private readonly struct RegistryMetadata
	{
		public required IReadOnlyList<PropertyInfo> Imports { get; init; }
		public required IReadOnlyList<PropertyInfo> Repositories { get; init; }
		public required IReadOnlyList<PropertyInfo> AutoIndices { get; init; }
	}

	private static void GetRegistryMetadata(Type registryType, out RegistryMetadata metadata)
	{
		lock (_metadataLock)
		{
			if (_metadata.TryGetValue(registryType, out metadata))
			{
				return;
			}
			
			List<PropertyInfo> imports = [];
			List<PropertyInfo> repositories = [];
			List<PropertyInfo> autoIndices = [];

			foreach (var property in registryType.GetProperties())
			{
				Type propertyType = property.PropertyType;
				if (property.GetCustomAttribute<ImportAttribute>() is not null)
				{
					if (property.SetMethod is null)
					{
						throw new InvalidOperationException();
					}
					
					if (!propertyType.IsAssignableTo(typeof(IRegistry)))
					{
						throw new InvalidOperationException();
					}
				
					imports.Add(property);
				}
				else if (property.GetCustomAttribute<RepositoryAttribute>() is not null)
				{
					if (property.SetMethod is null)
					{
						throw new InvalidOperationException();
					}
					
					if (!propertyType.GetInterfaces().Append(propertyType).Any(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IRepository<,>)))
					{
						throw new InvalidOperationException();
					}
				
					repositories.Add(property);
				}
				else if (property.GetCustomAttribute<AutoIndexAttribute>() is not null)
				{
					if (property.SetMethod is null)
					{
						throw new InvalidOperationException();
					}
					
					if (!propertyType.IsAssignableTo(typeof(IIndex)))
					{
						throw new InvalidOperationException();
					}
				
					autoIndices.Add(property);
				}
			}

			metadata = new()
			{
				Imports = imports,
				Repositories = repositories,
				AutoIndices = autoIndices,
			};
			_metadata[registryType] = metadata;
		}
	}

	private static readonly Dictionary<Type, RegistryMetadata> _metadata = new();
	private static readonly Lock _metadataLock = new();

}


