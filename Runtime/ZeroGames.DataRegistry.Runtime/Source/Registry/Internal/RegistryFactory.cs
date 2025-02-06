// Copyright Zero Games. All Rights Reserved.

using System.Reflection;
using System.Xml.Linq;

namespace ZeroGames.DataRegistry.Runtime;

public class RegistryFactory<T> : IRegistryFactory<T> where T : class, IRegistry, new()
{
	
	public T Create(IXDocumentProvider sourceProvider, IEnumerable<IRegistry> imports)
	{
		XDocument document = sourceProvider.Document;
		Dictionary<string, IRegistry> importMap = imports.ToDictionary(import => import.Name);

		T registry = new();
		var notifyInitialization = registry as INotifyInitialization;
		notifyInitialization?.PreInitialize();

		foreach (var importProperty in _metadata.Imports)
		{
			Type propertyType = importProperty.PropertyType;
			string name = propertyType.Name;
			if (!importMap.TryGetValue(name, out var import))
			{
				throw new KeyNotFoundException($"Import registry '{name}' not found.");
			}

			if (!import.GetType().IsAssignableTo(propertyType))
			{
				throw new ArgumentException($"Import registry '{name}' is not assignable to property with type '{propertyType}'.");
			}
			
			importProperty.SetValue(registry, import);
		}

		RepositoryFactory factory = new()
		{
			SerializerMap = new Dictionary<Type, Func<XElement, object>>(),
		};
		List<Action<Func<Type, object, object>>> finishInitializations = [];
		foreach (var repositoryProperty in _metadata.Repositories)
		{
			Type propertyType = repositoryProperty.PropertyType;
			Type[] propertyTypeParameters = propertyType.GetInterfaces().Append(propertyType).First(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IRepository<,>)).GetGenericArguments();
			Type entityType = propertyTypeParameters[1];
			XElement? root = document.Root?.Element($"{entityType.Name}Repository");
			if (root is null)
			{
				throw new InvalidOperationException();
			}
			
			object repository = factory.Create(registry, entityType, root, out var finishInitialization);
			if (!repository.GetType().IsAssignableTo(propertyType))
			{
				throw new InvalidOperationException();
			}
			
			repositoryProperty.SetValue(registry, repository);
			
			finishInitializations.Add(finishInitialization);
		}

		Func<Type, object, object> getEntity = (type, primaryKey) =>
		{
			throw new NotImplementedException();
		};
		
		foreach (var finishInitialization in finishInitializations)
		{
			finishInitialization(getEntity);
		}

		foreach (var autoIndex in _metadata.AutoIndices)
		{
			throw new NotImplementedException();
		}
		
		notifyInitialization?.PostInitialize();
		return registry;
	}

	private readonly struct RegistryMetadata
	{
		public required IReadOnlyList<PropertyInfo> Imports { get; init; }
		public required IReadOnlyList<PropertyInfo> Repositories { get; init; }
		public required IReadOnlyList<PropertyInfo> AutoIndices { get; init; }
	}

	static RegistryFactory()
	{
		Type registryType = typeof(T);

		List<PropertyInfo> imports = [];
		List<PropertyInfo> repositories = [];
		List<PropertyInfo> autoIndices = [];

		foreach (var property in registryType.GetProperties())
		{
			Type propertyType = property.PropertyType;
			if (property.SetMethod is null)
			{
				throw new InvalidOperationException();
			}
			
			if (property.GetCustomAttribute<ImportAttribute>() is not null)
			{
				if (!propertyType.IsAssignableTo(typeof(IRegistry)))
				{
					throw new InvalidOperationException();
				}
				
				imports.Add(property);
			}
			else if (property.GetCustomAttribute<RepositoryAttribute>() is not null)
			{
				if (!propertyType.GetInterfaces().Append(propertyType).Any(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IRepository<,>)))
				{
					throw new InvalidOperationException();
				}
				
				repositories.Add(property);
			}
			else if (property.GetCustomAttribute<AutoIndexAttribute>() is not null)
			{
				if (!propertyType.IsAssignableTo(typeof(IIndex)))
				{
					throw new InvalidOperationException();
				}
				
				autoIndices.Add(property);
			}
		}

		_metadata = new()
		{
			Imports = imports,
			Repositories = repositories,
			AutoIndices = autoIndices,
		};
	}

	private static readonly RegistryMetadata _metadata;

}


