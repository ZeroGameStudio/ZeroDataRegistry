// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;
using ZeroGames.DataRegistry.Compiler.Core;

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;


public partial class XmlCompilerFrontend
{
	
	
	private IReadOnlyList<IUserDefinedDataType> ParseDataTypes(XElement schemaElement, ISchema schema)
	{
		List<IUserDefinedDataType> result = new();
		foreach (var element in schemaElement.Elements())
		{
			if (element.Name == _usingElementName || element.Name == _metadataElementName)
			{
				continue;
			}
			
			result.Add(ParseDataType(element, schema));
		}

		return result;
	}
	
	private IUserDefinedDataType ParseDataType(XElement typeElement, ISchema schema)
	{
		if (typeElement.Name == _entityElementName)
		{
			return ParseEntity(typeElement, schema);
		}
		else if (typeElement.Name == _structElementName)
		{
			return ParseStruct(typeElement, schema);
		}
		else if (typeElement.Name == _enumElementName)
		{
			return ParseEnum(typeElement, schema);
		}
		else
		{
			throw new ParserException();
		}
	}
	
	private IEntityDataType ParseEntity(XElement entityElement, ISchema schema)
	{
		IReadOnlyList<IProperty> properties = ParseProperties(entityElement, schema);
		IReadOnlyList<IProperty> primaryKey = properties.Where(property => property.Role == EPropertyRole.PrimaryKey).ToArray();
		return new EntityDataType
		{
			Schema = schema,
			Name = GetName(entityElement),
			Namespace = GetNamespace(entityElement),
			PrimaryKey = primaryKey,
			BaseTypeFactory = ParseBaseType<IEntityDataType>(entityElement, schema),
			Properties = properties,
			Metadatas = ParseMetadatas(entityElement, schema),
		};
	}
	
	private IStructDataType ParseStruct(XElement structElement, ISchema schema)
	{
		return new StructDataType
		{
			Schema = schema,
			Name = GetName(structElement),
			Namespace =  GetNamespace(structElement),
			BaseTypeFactory = ParseBaseType<IStructDataType>(structElement, schema),
			Properties = ParseProperties(structElement, schema),
			Metadatas = ParseMetadatas(structElement, schema),
		};
	}

	private Func<T?> ParseBaseType<T>(XElement compositeTypeElement, ISchema typeSchema) where T : class, ICompositeDataType
	{
		string? path = compositeTypeElement.Attribute(_extendsAttributeName)?.Value;
		if (string.IsNullOrWhiteSpace(path))
		{
			return () => null;
		}

		string[] nodes = path.Split('.');
		ISchema schema = nodes.Length == 2 ? CompilationContext.GetSchema(nodes[0]) : typeSchema;
		string typeName = nodes.Last();
		return () => schema.DataTypes.OfType<T>().Single(type => type.Name == typeName);
	}

	private IEnumDataType ParseEnum(XElement enumElement, ISchema schema)
	{
		string? underlyingTypeName = enumElement.Attribute(_extendsAttributeName)?.Value ?? _options.DefaultEnumUnderlyingTypeName;
		if (string.IsNullOrWhiteSpace(underlyingTypeName))
		{
			throw new ParserException();
		}
		
		IPrimitiveDataType underlyingType = CompilationContext.GetPrimitiveDataType(underlyingTypeName);
		if (underlyingType.PrimitiveType > EPrimitiveType.Int64)
		{
			throw new ParserException();
		}
		
		return new EnumDataType
		{
			Schema = schema,
			Name = GetName(enumElement),
			Namespace =  GetNamespace(enumElement),
			UnderlyingType = underlyingType,
			Elements = ParseEnumElements(enumElement, schema, underlyingType),
			Metadatas = ParseMetadatas(enumElement, schema),
		};
	}

	private IReadOnlyList<IEnumElement> ParseEnumElements(XElement enumTypeElement, ISchema schema, IPrimitiveDataType underlyingType)
		=> enumTypeElement.Elements(_enumElementElementName).Select(element => ParseEnumElement(element, schema, GetEnumUnderlyingTypeRange(underlyingType))).ToArray();

	private (Int128, Int128) GetEnumUnderlyingTypeRange(IPrimitiveDataType underlyingType)
		=> underlyingType.PrimitiveType switch
		{
			EPrimitiveType.UInt8 => (uint8.MinValue, uint8.MaxValue),
			EPrimitiveType.UInt16 => (uint16.MinValue, uint16.MaxValue),
			EPrimitiveType.UInt32 => (uint32.MinValue, uint32.MaxValue),
			EPrimitiveType.UInt64 => (uint64.MinValue, uint64.MaxValue),
			EPrimitiveType.Int8 => (int8.MinValue, int8.MaxValue),
			EPrimitiveType.Int16 => (int16.MinValue, int16.MaxValue),
			EPrimitiveType.Int32 => (int32.MinValue, int32.MaxValue),
			EPrimitiveType.Int64 => (int64.MinValue, int64.MaxValue),
			_ => throw new ParserException()
		};
	
	private IEnumElement ParseEnumElement(XElement element, ISchema schema, (Int128, Int128) range)
	{
		return new EnumElement
		{
			Schema = schema,
			Name = GetName(element),
			Value = GetEnumValue(element, range),
			Metadatas = ParseMetadatas(element, schema),
		};
	}
	
	private Int128 GetEnumValue(XElement enumElementElement, (Int128 Min, Int128 Max) range)
	{
		if (!Int128.TryParse(enumElementElement.Attribute(_valueAttributeName)?.Value, out var value))
		{
			throw new ParserException();
		}

		if (value < range.Min || value > range.Max)
		{
			throw new ParserException();
		}
		
		return value;
	}
	
}


