// Copyright Zero Games. All Rights Reserved.

using System.Collections;
using System.Reflection;
using System.Xml.Linq;

namespace ZeroGames.DataRegistry.Runtime;

internal class RepositoryFactory
{
	
	public object Create(IRegistry registry, Type entityType, XElement source, out Action<Func<Type, object, object>> finishInitialization)
	{
		GetEntityMetadata(entityType, out var metadata);
		Type primaryKeyType = metadata.PrimaryKeyType;
		Type repositoryType = typeof(Repository<,>).MakeGenericType(primaryKeyType, entityType);
		var repository = (IDynamicRepository?)Activator.CreateInstance(repositoryType);
		if (repository is null)
		{
			throw new InvalidOperationException();
		}

		repositoryType.GetProperty(nameof(IRegistryElement.Registry))!.SetValue(repository, registry);
		repositoryType.GetProperty(nameof(IRegistryElement.Name))!.SetValue(repository, $"{entityType.Name}Repository");
		
		(repository as INotifyInitialization)?.PreInitialize();

		List<(XElement Source, object Entity)> entities = [];
		foreach (var entityElement in source.Elements())
		{
			if (entityElement.Name != entityType.Name)
			{
				throw new InvalidOperationException();
			}
			
			object? entity = Activator.CreateInstance(entityType);
			if (entity is null)
			{
				throw new InvalidOperationException();
			}
			
			entities.Add((entityElement, entity));

			(entity as INotifyInitialization)?.PreInitialize();
			
			List<object> primaryKeyComponents = [];
			foreach (var primaryKeyComponentProperty in metadata.PrimaryKeyComponents)
			{
				XElement? element = entityElement.Element(primaryKeyComponentProperty.Name);
				if (element is null)
				{
					throw new InvalidOperationException();
				}
				
				object value = Serialize(primaryKeyComponentProperty.PropertyType, element);
				primaryKeyComponentProperty.SetValue(entity, value);
				primaryKeyComponents.Add(value);
			}
			object primaryKey = MakeCompositeKey(primaryKeyComponents);

			foreach (var property in metadata.Properties)
			{
				XElement element = entityElement.Element(property.Name) ?? new(property.Name, property.GetCustomAttribute<PropertyAttribute>()?.Default);
				object value = Serialize(property.PropertyType, element);
				property.SetValue(entity, value);
			}
			
			repository.RegisterEntity(primaryKey, entity);
		}

		finishInitialization = getEntity =>
		{
			foreach (var (entityElement, entity) in entities)
			{
				foreach (var entityReference in metadata.EntityReferences)
				{
					Type referencedEntityType = entityReference.PropertyType;
					GetEntityMetadata(referencedEntityType, out var referenceMetadata);
					XElement? referencedEntityElement = entityElement.Element(entityReference.Name);
					if (referencedEntityElement is null)
					{
						throw new InvalidOperationException();
					}
					
					List<object> primaryKeyComponents = [];
					foreach (var primaryKeyComponentProperty in referenceMetadata.PrimaryKeyComponents)
					{
						XElement? element = referencedEntityElement.Element(primaryKeyComponentProperty.Name);
						if (element is null)
						{
							throw new InvalidOperationException();
						}
				
						object value = Serialize(primaryKeyComponentProperty.PropertyType, element);
						primaryKeyComponents.Add(value);
					}
					object primaryKey = MakeCompositeKey(primaryKeyComponents);
					object referencedEntity = getEntity(referencedEntityType, primaryKey);
					entityReference.SetValue(entity, referencedEntity);
				}
				
				(entity as INotifyInitialization)?.PostInitialize();
			}

			(repository as INotifyInitialization)?.PostInitialize();
		};

		return repository;
	}
	
	public required IReadOnlyDictionary<Type, Func<XElement, object>> SerializerMap { private get; init; }

	private readonly struct EntityMetadata
	{
		public required IReadOnlyList<PropertyInfo> PrimaryKeyComponents { get; init; }
		public required IReadOnlyList<PropertyInfo> Properties { get; init; }
		public required IReadOnlyList<PropertyInfo> EntityReferences { get; init; }

		public Type PrimaryKeyType => PrimaryKeyComponents.Count switch
		{
			1 => PrimaryKeyComponents[0].PropertyType,
			2 => typeof(ValueTuple<,>).MakeGenericType(PrimaryKeyComponents.Select(property => property.PropertyType).ToArray()),
			3 => typeof(ValueTuple<,,>).MakeGenericType(PrimaryKeyComponents.Select(property => property.PropertyType).ToArray()),
			4 => typeof(ValueTuple<,,,>).MakeGenericType(PrimaryKeyComponents.Select(property => property.PropertyType).ToArray()),
			5 => typeof(ValueTuple<,,,,>).MakeGenericType(PrimaryKeyComponents.Select(property => property.PropertyType).ToArray()),
			6 => typeof(ValueTuple<,,,,,>).MakeGenericType(PrimaryKeyComponents.Select(property => property.PropertyType).ToArray()),
			7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(PrimaryKeyComponents.Select(property => property.PropertyType).ToArray()),
			_ => throw new NotSupportedException("Primary key more than 7-dimension is not supported."),
		};
	}

	private static object MakeCompositeKey(IReadOnlyList<object> components) => components.Count switch
	{
		1 => components[0],
		2 => (components[0], components[1]),
		3 => (components[0], components[1], components[2]),
		4 => (components[0], components[1], components[2], components[3]),
		5 => (components[0], components[1], components[2], components[3], components[4]),
		6 => (components[0], components[1], components[2], components[3], components[4], components[5]),
		7 => (components[0], components[1], components[2], components[3], components[4], components[5], components[6]),
		_ => throw new NotSupportedException("Primary key more than 7-dimension is not supported."),
	};

	private object Serialize(Type type, XElement source)
	{
		if (type.GetInterfaces().Append(type).FirstOrDefault(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)) is {} genericListType)
		{
			Type elementType = genericListType.GetGenericArguments()[0];
			Type instancedListType = typeof(List<>).MakeGenericType(elementType);
			var container = (IList)Activator.CreateInstance(instancedListType)!;
			foreach (var element in source.Elements())
			{
				if (element.Name != elementType.Name)
				{
					throw new InvalidOperationException();
				}

				object value = RawSerialize(elementType, element);
				container.Add(value);
			}

			return container;
		}
		else if (type.GetInterfaces().Append(type).FirstOrDefault(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IReadOnlySet<>)) is {} genericSetType)
		{
			Type elementType = genericSetType.GetGenericArguments()[0];
			Type instancedSetType = typeof(HashSet<>).MakeGenericType(elementType);
			object container = Activator.CreateInstance(instancedSetType)!;
			MethodInfo addMethod = instancedSetType.GetMethod("Add")!;
			foreach (var element in source.Elements())
			{
				if (element.Name != elementType.Name)
				{
					throw new InvalidOperationException();
				}

				object value = RawSerialize(elementType, element);
				addMethod.Invoke(container, [ value ]);
			}

			return container;
		}
		else if (type.GetInterfaces().Append(type).FirstOrDefault(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)) is {} genericMapType)
		{
			Type keyType = genericMapType.GetGenericArguments()[0];
			Type valueType = genericMapType.GetGenericArguments()[1];
			Type instancedMapType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
			var container = (IDictionary)Activator.CreateInstance(instancedMapType)!;
			foreach (var element in source.Elements())
			{
				if (element.Name != "KeyValuePair")
				{
					throw new InvalidOperationException();
				}

				object key = RawSerialize(keyType, element.Element("Key")!);
				object value = RawSerialize(valueType, element.Element("Value")!);
				container[key] = value;
			}

			return container;
		}
		else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			Type elementType = type.GetGenericArguments()[0];
			return RawSerialize(elementType, source);
		}

		return RawSerialize(type, source);
	}

	private object RawSerialize(Type type, XElement source)
	{
		if (type.IsEnum)
		{
			throw new NotImplementedException();
		}
		
		if (!SerializerMap.TryGetValue(type, out var serializer))
		{
			serializer = _fallbackSerializerMap[type];
		}

		return serializer(source);
	}

	private void GetEntityMetadata(Type entityType, out EntityMetadata metadata)
	{
		lock (_metadataLock)
		{
			if (_metadata.TryGetValue(entityType, out metadata))
			{
				return;
			}
			
			List<PropertyInfo> primaryKeyComponents = [];
			List<PropertyInfo> properties = [];
			List<PropertyInfo> entityReferences = [];

			foreach (var property in entityType.GetProperties())
			{
				throw new NotImplementedException();
				Type propertyType = property.PropertyType;
				if (property.GetCustomAttribute<PrimaryKeyAttribute>() is not null)
				{
					primaryKeyComponents.Add(property);
				}
				else if (property.GetCustomAttribute<PropertyAttribute>() is not null)
				{
					if (propertyType.IsAssignableTo(typeof(IEntity)))
					{
						entityReferences.Add(property);
					}
					else
					{
						properties.Add(property);
					}
				}
			}

			metadata = new()
			{
				PrimaryKeyComponents = primaryKeyComponents,
				Properties = properties,
				EntityReferences = entityReferences,
			};
			_metadata[entityType] = metadata;
		}
	}

	private static readonly IReadOnlyDictionary<Type, Func<XElement, object>> _fallbackSerializerMap = new Dictionary<Type, Func<XElement, object>>
	{
		[typeof(uint8)] = element => uint8.Parse(element.Value),
		[typeof(uint16)] = element => uint16.Parse(element.Value),
		[typeof(uint32)] = element => uint32.Parse(element.Value),
		[typeof(uint64)] = element => uint64.Parse(element.Value),
		[typeof(int8)] = element => int8.Parse(element.Value),
		[typeof(int16)] = element => int16.Parse(element.Value),
		[typeof(int32)] = element => int32.Parse(element.Value),
		[typeof(int64)] = element => int64.Parse(element.Value),
		[typeof(float)] = element => float.Parse(element.Value),
		[typeof(double)] = element => double.Parse(element.Value),
		[typeof(bool)] = element => bool.Parse(element.Value),
		[typeof(string)] = element => element.Value,
	};
	
	private static readonly Dictionary<Type, EntityMetadata> _metadata = new();
	private static readonly Lock _metadataLock = new();
	
}


