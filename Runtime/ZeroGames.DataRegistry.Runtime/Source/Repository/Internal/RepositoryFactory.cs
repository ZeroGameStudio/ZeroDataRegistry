// Copyright Zero Games. All Rights Reserved.

using System.Collections;
using System.Reflection;
using System.Xml.Linq;

namespace ZeroGames.DataRegistry.Runtime;

internal class RepositoryFactory
{

	public delegate Type GetElementTypeDelegate(Type rootType, XElement objectElement);
	public delegate object? GetEntityDelegate(Type type, object primaryKey);
	public delegate void FinishInitializationDelegate(GetElementTypeDelegate getElementType, GetEntityDelegate getEntity);
	
	public IInitializingRepository Create(IRegistry registry, Type entityType, XElement repositoryElement, out FinishInitializationDelegate finishInitialization)
	{
		GetEntityMetadata(entityType, out var metadata);
		Type primaryKeyType = metadata.PrimaryKeyType;
		Type repositoryType = typeof(Repository<,>).MakeGenericType(primaryKeyType, entityType);
		var repository = (IInitializingRepository?)Activator.CreateInstance(repositoryType);
		if (repository is null)
		{
			throw new InvalidOperationException();
		}

		repositoryType.GetProperty(nameof(IRegistryElement.Registry))!.SetValue(repository, registry);
		repositoryType.GetProperty(nameof(IRegistryElement.Name))!.SetValue(repository, $"{entityType.Name}Repository");
		
		(repository as INotifyInitialization)?.PreInitialize();

		List<(XElement Source, object Entity)> pendingInitializedEntities = [];
		foreach (var entityElement in repositoryElement.Elements())
		{
			if (entityElement.Name != entityType.Name)
			{
				throw new InvalidOperationException();
			}
			
			var entity = (IEntity?)Activator.CreateInstance(entityType);
			if (entity is null)
			{
				throw new InvalidOperationException();
			}

			MakePrimaryKey(metadata, entityElement, static (property, value, state) => property.SetValue(state, value), entity);
			repository.RegisterEntity(entity);
			pendingInitializedEntities.Add((entityElement, entity));
		}

		finishInitialization = (getElementType, getEntity) =>
		{
			foreach (var (entityElement, entity) in pendingInitializedEntities)
			{
				(entity as INotifyInitialization)?.PreInitialize();
				
				foreach (var property in metadata.Properties)
				{
					XElement propertyElement = GetPropertyElement(property, entityElement);
					object? value = Serialize(property.PropertyType, propertyElement, getElementType, getEntity);
					if (!property.IsNullable() && value is null)
					{
						throw new InvalidOperationException();
					}
					
					property.SetValue(entity, value);
				}
				
				(entity as INotifyInitialization)?.PostInitialize();
			}

			(repository as INotifyInitialization)?.PostInitialize();
		};

		return repository;
	}
	
	public required IReadOnlyDictionary<Type, Func<string, object>> PrimitiveSerializerMap { private get; init; }
	
	private readonly struct EntityMetadata
	{
		public required IReadOnlyList<PropertyInfo> PrimaryKeyComponents { get; init; }
		public required IReadOnlyList<PropertyInfo> Properties { get; init; }

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
	
	private static void GetEntityMetadata(Type entityType, out EntityMetadata metadata)
	{
		lock (_metadataLock)
		{
			if (_metadata.TryGetValue(entityType, out metadata))
			{
				return;
			}
			
			List<PropertyInfo> primaryKeyComponents = [];
			List<PropertyInfo> properties = [];

			foreach (var property in entityType.GetProperties())
			{
				if (property.GetCustomAttribute<PrimaryKeyAttribute>() is not null)
				{
					primaryKeyComponents.Add(property);
				}
				else if (property.GetCustomAttribute<PropertyAttribute>() is not null)
				{
					properties.Add(property);
				}
			}

			metadata = new()
			{
				PrimaryKeyComponents = primaryKeyComponents,
				Properties = properties,
			};
			_metadata[entityType] = metadata;
		}
	}

	private object MakePrimaryKey(in EntityMetadata metadata, XElement entityElement, Action<PropertyInfo, object, object?>? onComponentSerialized, object? state)
	{
		int32 count = metadata.PrimaryKeyComponents.Count;
		var components = new object[count];
		int32 i = 0;
		foreach (var property in metadata.PrimaryKeyComponents)
		{
			XElement? propertyElement = entityElement.Element(property.Name);
			if (propertyElement is null)
			{
				throw new InvalidOperationException();
			}
				
			object value = SerializePrimitive(property.PropertyType, propertyElement.Value);
			onComponentSerialized?.Invoke(property, value, state);
			components[i++] = value;
		}

		return components.Length > 1 ? Activator.CreateInstance(metadata.PrimaryKeyType, components)! : components[0];
	}
	
	private object MakePrimaryKey(in EntityMetadata metadata, ReadOnlySpan<string> rawComponents)
	{
		int32 count = metadata.PrimaryKeyComponents.Count;
		var components = new object[count];
		int32 i = 0;
		foreach (var property in metadata.PrimaryKeyComponents)
		{
			object value = SerializePrimitive(property.PropertyType, rawComponents[i]);
			components[i++] = value;
		}

		return components.Length > 1 ? Activator.CreateInstance(metadata.PrimaryKeyType, components)! : components[0];
	}

	private object? Serialize(Type type, XElement propertyElement, GetElementTypeDelegate getElementType, GetEntityDelegate getEntity)
	{
		if (type.GetGenericInstanceOf(typeof(IReadOnlyList<>)) is {} genericListType)
		{
			Type elementType = genericListType.GetGenericArguments()[0];
			Type instancedListType = typeof(List<>).MakeGenericType(elementType);
			var container = (IList)Activator.CreateInstance(instancedListType)!;
			foreach (var element in propertyElement.Elements())
			{
				if (element.Name != CONTAINER_ELEMENT_ELEMENT_NAME)
				{
					throw new InvalidOperationException();
				}
				
				object? value = SerializeNonContainer(elementType, element, getElementType, getEntity);
				if (value is null)
				{
					throw new InvalidOperationException();
				}
				
				container.Add(value);
			}

			return container;
		}
		else if (type.GetGenericInstanceOf(typeof(IReadOnlySet<>)) is {} genericSetType)
		{
			Type elementType = genericSetType.GetGenericArguments()[0];
			Type instancedSetType = typeof(HashSet<>).MakeGenericType(elementType);
			object container = Activator.CreateInstance(instancedSetType)!;
			MethodInfo addMethod = instancedSetType.GetMethod(HASHSET_ADD_METHOD_NAME)!;
			foreach (var element in propertyElement.Elements())
			{
				if (element.Name != CONTAINER_ELEMENT_ELEMENT_NAME)
				{
					throw new InvalidOperationException();
				}

				object? value = SerializeNonContainer(elementType, element, getElementType, getEntity);
				if (value is null)
				{
					throw new InvalidOperationException();
				}
				
				addMethod.Invoke(container, [ value ]);
			}

			return container;
		}
		else if (type.GetGenericInstanceOf(typeof(IReadOnlyDictionary<,>)) is {} genericMapType)
		{
			Type keyType = genericMapType.GetGenericArguments()[0];
			Type valueType = genericMapType.GetGenericArguments()[1];
			Type instancedMapType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
			var container = (IDictionary)Activator.CreateInstance(instancedMapType)!;
			foreach (var element in propertyElement.Elements())
			{
				if (element.Name != CONTAINER_ELEMENT_ELEMENT_NAME)
				{
					throw new InvalidOperationException();
				}
				
				object? key = SerializeNonContainer(keyType, element.Element(MAP_KEY_ELEMENT_NAME)!, getElementType, getEntity);
				object? value = SerializeNonContainer(valueType, element.Element(MAP_VALUE_ELEMENT_NAME)!, getElementType, getEntity);
				if (key is null || value is null)
				{
					throw new InvalidOperationException();
				}
				
				container[key] = value;
			}

			return container;
		}
		else if (Nullable.GetUnderlyingType(type) is {} underlyingValueType)
		{
			return SerializeNonContainer(underlyingValueType, propertyElement, getElementType, getEntity);
		}

		return SerializeNonContainer(type, propertyElement, getElementType, getEntity);
	}

	private object? SerializeNonContainer(Type type, XElement propertyElement, GetElementTypeDelegate getElementType, GetEntityDelegate getEntity)
	{
		if (propertyElement.IsEmpty)
		{
			return null;
		}
		
		if (type.IsEnum)
		{
			return Enum.Parse(type, propertyElement.Value);
		}
		else if (type.IsAssignableTo(typeof(IStruct)))
		{
			XElement structElement = propertyElement.Elements().Single();
			Type implementationType = getElementType(type, structElement);
			object? instance = Activator.CreateInstance(implementationType);
			if (instance is null)
			{
				throw new InvalidOperationException();
			}
			
			foreach (var property in implementationType.GetProperties().Where(p => p.GetCustomAttribute<PropertyAttribute>() is not null))
			{
				XElement innerPropertyElement = GetPropertyElement(property, structElement);
				object? value = Serialize(property.PropertyType, innerPropertyElement, getElementType, getEntity);
				if (!property.IsNullable() && value is null)
				{
					throw new InvalidOperationException();
				}
				
				property.SetValue(instance, value);
			}
			
			return instance;
		}
		else if (type.IsAssignableTo(typeof(IEntity)))
		{
			XElement entityReferenceElement = propertyElement.Elements().Single();
			Type implementationType = getElementType(type, entityReferenceElement);
			GetEntityMetadata(implementationType, out var metadata);
			string[] rawComponents = entityReferenceElement.Value.Split(entityReferenceElement.Attribute(SEP_ATTRIBUTE_NAME)?.Value ?? ",");
			object primaryKey = MakePrimaryKey(metadata, rawComponents);
			return getEntity(implementationType, primaryKey);
		}
		
		return SerializePrimitive(type, propertyElement.Value);
	}

	private object SerializePrimitive(Type type, string value)
	{
		if (!PrimitiveSerializerMap.TryGetValue(type, out var serializer))
		{
			serializer = _fallbackPrimitiveSerializerMap[type];
		}

		return serializer(value);
	}
	
	private XElement GetPropertyElement(PropertyInfo property, XElement targetElement)
		=> targetElement.Element(property.Name) ?? new(property.Name, property.GetCustomAttribute<PropertyAttribute>()?.Default);

	private const string CONTAINER_ELEMENT_ELEMENT_NAME = "Element";
	private const string MAP_KEY_ELEMENT_NAME = "Key";
	private const string MAP_VALUE_ELEMENT_NAME = "Value";

	private const string SEP_ATTRIBUTE_NAME = "Sep";
	private const string EXTENDS_ATTRIBUTE_NAME = "Extends";
	private const string EXTENDS_SEP_ATTRIBUTE_NAME = "ExtendsSep";
	
	private const string HASHSET_ADD_METHOD_NAME = "Add";

	private static readonly IReadOnlyDictionary<Type, Func<string, object>> _fallbackPrimitiveSerializerMap = new Dictionary<Type, Func<string, object>>
	{
		[typeof(uint8)] = value => uint8.Parse(value),
		[typeof(uint16)] = value => uint16.Parse(value),
		[typeof(uint32)] = value => uint32.Parse(value),
		[typeof(uint64)] = value => uint64.Parse(value),
		[typeof(int8)] = value => int8.Parse(value),
		[typeof(int16)] = value => int16.Parse(value),
		[typeof(int32)] = value => int32.Parse(value),
		[typeof(int64)] = value => int64.Parse(value),
		[typeof(float)] = value => float.Parse(value),
		[typeof(double)] = value => double.Parse(value),
		[typeof(bool)] = value => bool.Parse(value),
		[typeof(string)] = value => value,
	};
	
	private static readonly Dictionary<Type, EntityMetadata> _metadata = new();
	private static readonly Lock _metadataLock = new();
	
}


