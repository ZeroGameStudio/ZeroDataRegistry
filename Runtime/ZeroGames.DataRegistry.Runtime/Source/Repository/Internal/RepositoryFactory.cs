// Copyright Zero Games. All Rights Reserved.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
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

		Dictionary<IEntity, XElement> pendingInitializedEntities = [];
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
			
			foreach (var property in metadata.PrimaryKeyComponents)
			{
				XElement? propertyElement = entityElement.Element(property.Name);
				if (propertyElement is null)
				{
					throw new InvalidOperationException();
				}
				
				object value = SerializePrimitive(property.PropertyType, propertyElement.Value);
				property.SetValue(entity, value);
			}

			repository.RegisterEntity(entity);
			pendingInitializedEntities[entity] = entityElement;
		}

		finishInitialization = (getElementType, getEntity) =>
		{
			HashSet<IEntity> initializedEntities = [];
			Dictionary<IEntity, Dictionary<PropertyInfo, XElement?>> entityPropertyElementLookup = [];

			void InitializeEntity(IEntity entity, XElement entityElement)
			{
				if (!initializedEntities.Add(entity))
				{
					return;
				}
				
				// If entity extends another entity, then the base entity must get initialized first (recursively).
				string? baseEntityReference = entityElement.Attribute(EXTENDS_ATTRIBUTE_NAME)?.Value;
				IEntity? baseEntity = null;
				if (!string.IsNullOrWhiteSpace(baseEntityReference))
				{
					string[] rawComponents = baseEntityReference.Split(entityElement.Attribute(EXTENDS_SEP_ATTRIBUTE_NAME)?.Value ?? DEFAULT_REFERENCE_SEP);
					object primaryKey = MakePrimaryKey(metadata, rawComponents);
					repository.TryGetEntity(primaryKey, out baseEntity);
					if (baseEntity is null || baseEntity.GetType() != entityType)
					{
						throw new InvalidOperationException();
					}
					
					InitializeEntity(baseEntity, pendingInitializedEntities[baseEntity]);
				}

				// Now all base entities on the inheritance chain is initialized so we can initialize this entity.
				bool @abstract = entityElement.Attribute(ABSTRACT_ATTRIBUTE_NAME) is { } abstractAttr && bool.Parse(abstractAttr.Value);
				if (@abstract)
				{
					entityType.GetProperty(nameof(IEntity.IsAbstract), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(entity, true);
				}

				if (!@abstract)
				{
					(entity as INotifyInitialization)?.PreInitialize();
				}
				
				foreach (var property in metadata.Properties)
				{
					XElement? propertyElement = entityElement.Element(property.Name);
					if (propertyElement is null)
					{
						if (!@abstract && baseEntity is null)
						{
							throw new InvalidOperationException();
						}

						if (baseEntity is not null)
						{
							try
							{
								propertyElement = entityPropertyElementLookup[baseEntity][property];
							}
							catch (KeyNotFoundException ex)
							{
								throw new InvalidOperationException($"Recursive inheritance detected on entity '{entity.PrimaryKey}'", ex);
							}
						}
					}

					if (!@abstract)
					{
						if (propertyElement is null)
						{
							throw new InvalidOperationException();
						}
						
						object? value = Serialize(property.PropertyType, propertyElement, property.IsNotNull() ? ReturnNotNull.True : ReturnNotNull.False, getElementType, getEntity);
						property.SetValue(entity, value);
					}
					
					if (!entityPropertyElementLookup.TryGetValue(entity, out var lookup))
					{
						lookup = [];
						entityPropertyElementLookup[entity] = lookup;
					}
					
					lookup[property] = propertyElement;
				}

				if (!@abstract)
				{
					(entity as INotifyInitialization)?.PostInitialize();
				}
			}
			
			foreach (var (entity, entityElement) in pendingInitializedEntities)
			{
				InitializeEntity(entity, entityElement);
			}

			(repository as INotifyInitialization)?.PostInitialize();
		};

		return repository;
	}
	
	public required IReadOnlyDictionary<Type, Func<string, object>> PrimitiveSerializerMap { private get; init; }

	private readonly struct ReturnNotNull
	{
		public static ReturnNotNull True => default;
		public static ReturnNotNull? False => null;
	}
	
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

	private object? Serialize(Type type, XElement propertyElement, ReturnNotNull? returnNotNullIfNotContainer, GetElementTypeDelegate getElementType, GetEntityDelegate getEntity)
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
				
				object value = SerializeNonContainer(elementType, element, ReturnNotNull.True, getElementType, getEntity);
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

				object value = SerializeNonContainer(elementType, element, ReturnNotNull.True, getElementType, getEntity);
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
				
				object key = SerializeNonContainer(keyType, element.Element(MAP_KEY_ELEMENT_NAME)!, ReturnNotNull.True, getElementType, getEntity);
				object value = SerializeNonContainer(valueType, element.Element(MAP_VALUE_ELEMENT_NAME)!, ReturnNotNull.True, getElementType, getEntity);
				container[key] = value;
			}

			return container;
		}
		else if (Nullable.GetUnderlyingType(type) is {} underlyingValueType)
		{
			return SerializeNonContainer(underlyingValueType, propertyElement, ReturnNotNull.False, getElementType, getEntity);
		}

		return SerializeNonContainer(type, propertyElement, returnNotNullIfNotContainer, getElementType, getEntity);
	}

	[return: NotNullIfNotNull(nameof(returnNotNull))]
	private object? SerializeNonContainer(Type type, XElement propertyElement, ReturnNotNull? returnNotNull, GetElementTypeDelegate getElementType, GetEntityDelegate getEntity)
	{
		bool notnull = returnNotNull is not null;
		bool empty = string.IsNullOrEmpty(propertyElement.Value);
		if (type.IsEnum)
		{
			if (empty)
			{
				return notnull ? throw new InvalidOperationException() : null;
			}
			
			return Enum.Parse(type, propertyElement.Value);
		}
		else if (type.IsAssignableTo(typeof(IStruct)))
		{
			if (empty)
			{
				return notnull ? throw new InvalidOperationException() : null;
			}
			
			XElement structElement = propertyElement.Elements().Single();
			Type implementationType = getElementType(type, structElement);
			object? instance = Activator.CreateInstance(implementationType);
			if (instance is null)
			{
				throw new InvalidOperationException();
			}
			
			foreach (var property in implementationType.GetProperties().Where(p => p.GetCustomAttribute<PropertyAttribute>() is not null))
			{
				Type propertyType = property.PropertyType;
				XElement? innerPropertyElement = structElement.Element(property.Name);
				if (innerPropertyElement is null)
				{
					throw new InvalidOperationException();
				}
				
				object? value = Serialize(propertyType, innerPropertyElement, property.IsNotNull() ? ReturnNotNull.True : ReturnNotNull.False, getElementType, getEntity);
				property.SetValue(instance, value);
			}
			
			return instance;
		}
		else if (type.IsAssignableTo(typeof(IEntity)))
		{
			if (empty)
			{
				return notnull ? throw new InvalidOperationException() : null;
			}
			
			XElement entityReferenceElement = propertyElement.Elements().Single();
			Type implementationType = getElementType(type, entityReferenceElement);
			GetEntityMetadata(implementationType, out var metadata);
			string[] rawComponents = entityReferenceElement.Value.Split(entityReferenceElement.Attribute(SEP_ATTRIBUTE_NAME)?.Value ?? DEFAULT_REFERENCE_SEP);
			object primaryKey = MakePrimaryKey(metadata, rawComponents);
			return getEntity(implementationType, primaryKey);
		}

		if (empty && !notnull)
		{
			return null;
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
	
	private const string CONTAINER_ELEMENT_ELEMENT_NAME = "Element";
	private const string MAP_KEY_ELEMENT_NAME = "Key";
	private const string MAP_VALUE_ELEMENT_NAME = "Value";

	private const string ABSTRACT_ATTRIBUTE_NAME = "Abstract";
	private const string EXTENDS_ATTRIBUTE_NAME = "Extends";
	private const string EXTENDS_SEP_ATTRIBUTE_NAME = "ExtendsSep";
	private const string SEP_ATTRIBUTE_NAME = "Sep";

	private const string HASHSET_ADD_METHOD_NAME = "Add";

	private const string DEFAULT_REFERENCE_SEP = ",";

	private static readonly IReadOnlyDictionary<Type, Func<string, object>> _fallbackPrimitiveSerializerMap = new Dictionary<Type, Func<string, object>>
	{
		[typeof(uint8)] = value => !string.IsNullOrWhiteSpace(value) ? uint8.Parse(value) : 0,
		[typeof(uint16)] = value => !string.IsNullOrWhiteSpace(value) ? uint16.Parse(value) : 0,
		[typeof(uint32)] = value => !string.IsNullOrWhiteSpace(value) ? uint32.Parse(value) : 0,
		[typeof(uint64)] = value => !string.IsNullOrWhiteSpace(value) ? uint64.Parse(value) : 0,
		[typeof(int8)] = value => !string.IsNullOrWhiteSpace(value) ? int8.Parse(value) : 0,
		[typeof(int16)] = value => !string.IsNullOrWhiteSpace(value) ? int16.Parse(value) : 0,
		[typeof(int32)] = value => !string.IsNullOrWhiteSpace(value) ? int32.Parse(value) : 0,
		[typeof(int64)] = value => !string.IsNullOrWhiteSpace(value) ? int64.Parse(value) : 0,
		[typeof(float)] = value => !string.IsNullOrWhiteSpace(value) ? float.Parse(value) : 0,
		[typeof(double)] = value => !string.IsNullOrWhiteSpace(value) ? double.Parse(value) : 0,
		[typeof(bool)] = value => !string.IsNullOrWhiteSpace(value) && bool.Parse(value),
		[typeof(string)] = value => value,
	};
	
	private static readonly Dictionary<Type, EntityMetadata> _metadata = new();
	private static readonly Lock _metadataLock = new();
	
}


