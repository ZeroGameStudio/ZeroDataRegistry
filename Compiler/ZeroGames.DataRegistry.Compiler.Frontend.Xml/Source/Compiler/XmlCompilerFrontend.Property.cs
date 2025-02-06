// Copyright Zero Games. All Rights Reserved.

using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

public partial class XmlCompilerFrontend
{

	private IReadOnlyList<IProperty> ParseProperties(XElement compositeTypeElement, ISchema schema)
	{
		bool requiresPrimaryKey = compositeTypeElement.Name == ENTITY_ELEMENT_NAME && compositeTypeElement.Attribute(EXTENDS_ATTRIBUTE_NAME) is null;
		bool primaryKeyFound = false;
		List<IProperty> result = new();
		foreach (var element in compositeTypeElement.Elements())
		{
			if (element.Name == METADATA_ELEMENT_NAME)
			{
				continue;
			}

			if (element.Name == PRIMARY_KEY_ELEMENT_NAME && !requiresPrimaryKey)
			{
				throw new ParserException();
			}

			primaryKeyFound = true;

			result.Add(ParseProperty(element, schema));
		}

		if (requiresPrimaryKey && !primaryKeyFound)
		{
			throw new ParserException();
		}

		return result;
	}
	
	private IProperty ParseProperty(XElement propertyElement, ISchema schema)
	{
		return new Property
		{
			Schema = schema,
			Role = GetPropertyRole(propertyElement),
			Name = GetName(propertyElement),
			TypeFactory = ParsePropertyType(propertyElement, schema),
			DefaultValue = propertyElement.Attribute(DEFAULT_ATTRIBUTE_NAME)?.Value ?? string.Empty,
			Metadatas = ParseMetadatas(propertyElement, schema),
		};
	}

	private EPropertyRole GetPropertyRole(XElement propertyElement)
	{
		if (propertyElement.Name == PROPERTY_ELEMENT_NAME)
		{
			return EPropertyRole.Default;
		}
		else if (propertyElement.Name == PRIMARY_KEY_ELEMENT_NAME)
		{
			return EPropertyRole.PrimaryKey;
		}
		else if (propertyElement.Name == FOREIGN_KEY_ELEMENT_NAME)
		{
			return EPropertyRole.ForeignKey;
		}
		else
		{
			throw new ParserException();
		}
	}
	
	private Func<IDataType> ParsePropertyType(XElement propertyElement, ISchema propertySchema)
	{
		string? path = propertyElement.Attribute(TYPE_ATTRIBUTE_NAME)?.Value;
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ParserException();
		}

		// List
		if (path == CompilationContext.GenericListType.Name)
		{
			string? valueTypePath = propertyElement.Attribute(VALUE_ATTRIBUTE_NAME)?.Value;
			if (string.IsNullOrWhiteSpace(valueTypePath))
			{
				throw new ParserException();
			}

			return ParseListContainerType(valueTypePath, propertyElement, propertySchema);
		}
		else if (path.EndsWith("[]"))
		{
			string valueTypePath = path.Substring(0, path.Length - 2);
			return ParseListContainerType(valueTypePath, propertyElement, propertySchema);
		}
		// Set
		else if (path == CompilationContext.GenericSetType.Name)
		{
			string? valueTypePath = propertyElement.Attribute(VALUE_ATTRIBUTE_NAME)?.Value;
			if (string.IsNullOrWhiteSpace(valueTypePath))
			{
				throw new ParserException();
			}

			return ParseSetContainerType(valueTypePath, propertyElement, propertySchema);
		}
		else if (path.EndsWith("{}"))
		{
			string valueTypePath = path.Substring(0, path.Length - 2);
			return ParseSetContainerType(valueTypePath, propertyElement, propertySchema);
		}
		// Map
		else if (path == CompilationContext.GenericMapType.Name)
		{
			string? keyTypePath = propertyElement.Attribute(KEY_ATTRIBUTE_NAME)?.Value;
			if (string.IsNullOrWhiteSpace(keyTypePath))
			{
				throw new ParserException();
			}
			
			string? valueTypePath = propertyElement.Attribute(VALUE_ATTRIBUTE_NAME)?.Value;
			if (string.IsNullOrWhiteSpace(valueTypePath))
			{
				throw new ParserException();
			}

			return ParseMapContainerType(keyTypePath, valueTypePath, propertyElement, propertySchema);
		}
		else if (_mapRegex.IsMatch(path))
		{
			string[] kv = path.Split("->");
			if (kv.Length != 2)
			{
				throw new ParserException();
			}
			
			string keyTypePath = kv[0];
			string valueTypePath = kv[1];
			return ParseMapContainerType(keyTypePath, valueTypePath, propertyElement, propertySchema);
		}
		// Optional
		else if (path == CompilationContext.GenericOptionalType.Name)
		{
			string? valueTypePath = propertyElement.Attribute(VALUE_ATTRIBUTE_NAME)?.Value;
			if (string.IsNullOrWhiteSpace(valueTypePath))
			{
				throw new ParserException();
			}

			return ParseOptionalContainerType(valueTypePath, propertyElement, propertySchema);
		}
		else if (path.EndsWith('?'))
		{
			string valueTypePath = path.Substring(0, path.Length - 1);
			return ParseOptionalContainerType(valueTypePath, propertyElement, propertySchema);
		}
		// Non-container
		else
		{
			return ParseNonContainerType(path, propertyElement, propertySchema);
		}
	}

	private Func<IInstancedContainerDataType> ParseListContainerType(string valueTypePath, XElement propertyElement, ISchema propertySchema)
	{
		Func<IDataType> valueTypeFactory = ParseNonContainerType(valueTypePath, propertyElement, propertySchema);
		return () => CompilationContext.GenericListType.Instantiate(CompilationContext.VoidDataType, valueTypeFactory());
	}
	
	private Func<IInstancedContainerDataType> ParseSetContainerType(string valueTypePath, XElement propertyElement, ISchema propertySchema)
	{
		Func<IDataType> valueTypeFactory = ParseNonContainerType(valueTypePath, propertyElement, propertySchema);
		return () => CompilationContext.GenericSetType.Instantiate(CompilationContext.VoidDataType, valueTypeFactory());
	}
	
	private Func<IInstancedContainerDataType> ParseMapContainerType(string keyTypePath, string valueTypePath, XElement propertyElement, ISchema propertySchema)
	{
		Func<IPrimitiveDataType> keyTypeFactory = () =>
		{
			IDataType keyType = ParseNonContainerType(keyTypePath, propertyElement, propertySchema)();
			if (keyType is not IPrimitiveDataType primitiveKeyType)
			{
				throw new ParserException();
			}

			return primitiveKeyType;
		};
		Func<IDataType> valueTypeFactory = ParseNonContainerType(valueTypePath, propertyElement, propertySchema);
		return () => CompilationContext.GenericMapType.Instantiate(keyTypeFactory(), valueTypeFactory());
	}
	
	private Func<IInstancedContainerDataType> ParseOptionalContainerType(string valueTypePath, XElement propertyElement, ISchema propertySchema)
	{
		Func<IDataType> valueTypeFactory = ParseNonContainerType(valueTypePath, propertyElement, propertySchema);
		return () => CompilationContext.GenericOptionalType.Instantiate(CompilationContext.VoidDataType, valueTypeFactory());
	}

	private Func<IDataType> ParseNonContainerType(string path, XElement propertyElement, ISchema propertySchema)
	{
		string[] nodes = path.Split('.');
		ISchema schema = nodes.Length == 2 ? propertySchema.ImportMap[nodes[0]] : propertySchema;
		string typeName = nodes.Last();
		return () =>
		{
			bool foreignKey = propertyElement.Name == FOREIGN_KEY_ELEMENT_NAME;
			IDataType type = schema.DataTypes.FirstOrDefault(type => type.Name == typeName) ?? (IDataType)CompilationContext.GetPrimitiveDataType(typeName);
			if (foreignKey ^ type is IEntityDataType)
			{
				throw new ParserException();
			}

			return type;
		};
	}
	
	[GeneratedRegex("^([A-Za-z_][A-Za-z0-9_]*)(\\.[A-Za-z_][A-Za-z0-9_]*)?->([A-Za-z_][A-Za-z0-9_]*)(\\.[A-Za-z_][A-Za-z0-9_]*)?$")]
	private static partial Regex _mapRegex { get; }
	
}


