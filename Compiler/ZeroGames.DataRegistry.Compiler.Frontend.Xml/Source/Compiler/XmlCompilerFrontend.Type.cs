// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

public partial class XmlCompilerFrontend
{
	
	private IReadOnlyList<IUserDefinedDataType> ParseDataTypes(XElement schemaElement, ISchema schema)
	{
		List<IUserDefinedDataType> result = new();
		foreach (var element in schemaElement.Elements())
		{
			if (element.Name == IMPORT_ELEMENT_NAME || element.Name == METADATA_ELEMENT_NAME)
			{
				continue;
			}
			
			result.Add(ParseDataType(element, schema));
		}

		return result;
	}
	
	private IUserDefinedDataType ParseDataType(XElement typeElement, ISchema schema)
	{
		if (typeElement.Name == ENTITY_ELEMENT_NAME)
		{
			return ParseEntity(typeElement, schema);
		}
		else if (typeElement.Name == STRUCT_ELEMENT_NAME)
		{
			return ParseStruct(typeElement, schema);
		}
		else if (typeElement.Name == INTERFACE_ELEMENT_NAME)
		{
			return ParseInterface(typeElement, schema);
		}
		else if (typeElement.Name == ENUM_ELEMENT_NAME)
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
		string? primaryKey = entityElement.Attribute(PRIMARY_KEY_ATTRIBUTE_NAME)?.Value;
		XAttribute? extendsAttribute = entityElement.Attribute(EXTENDS_ATTRIBUTE_NAME);
		if (primaryKey is null && extendsAttribute is null)
		{
			if (!string.IsNullOrWhiteSpace(_options.DefaultPrimaryKey))
			{
				primaryKey = _options.DefaultPrimaryKey;
			}

			if (primaryKey is null)
			{
				throw new ParserException("Entity primary key not found.");
			}
		}

		if (primaryKey is not null && extendsAttribute is not null)
		{
			throw new ParserException("Inherited entity defines primary key.");
		}

		if (primaryKey is not null && string.IsNullOrWhiteSpace(primaryKey))
		{
			throw new ParserException("Entity primary key is empty.");
		}
		
		string[] components = primaryKey is not null ? primaryKey.Replace(" ", "").Split(',') : [];
		HashSet<string> componentSet = components.ToHashSet();
		if (components.Length != componentSet.Count)
		{
			throw new ParserException("Duplicated primary key component.");
		}
		
		IReadOnlyList<IProperty> properties = ParseProperties(entityElement, schema, componentSet);
		IReadOnlyList<IProperty> primaryKeyComponents = properties.Where(property => componentSet.Contains(property.Name)).ToArray();
		if (primaryKeyComponents.Count != componentSet.Count)
		{
			throw new ParserException("Undefined primary key component.");
		}
		
		if (primaryKeyComponents.Count > 7)
		{
			throw new NotSupportedException("Primary key more than 7-dimension is not supported.");
		}
		
		return new EntityDataType
		{
			Schema = schema,
			Name = GetName(entityElement),
			Namespace = GetNamespace(entityElement),
			PrimaryKeyComponents = primaryKeyComponents,
			BaseTypeFactory = ParseBaseType<IEntityDataType>(entityElement, schema),
			InterfaceFactory = ParseImplementedInterfaces(entityElement, schema),
			IsAbstract = entityElement.Attribute(ABSTRACT_ATTRIBUTE_NAME) is {} abstractAttr && bool.Parse(abstractAttr.Value),
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
			InterfaceFactory = ParseImplementedInterfaces(structElement, schema),
			IsAbstract = structElement.Attribute(ABSTRACT_ATTRIBUTE_NAME) is {} abstractAttr && bool.Parse(abstractAttr.Value),
			Properties = ParseProperties(structElement, schema, []),
			Metadatas = ParseMetadatas(structElement, schema),
		};
	}
	
	private IInterfaceDataType ParseInterface(XElement interfaceElement, ISchema schema)
	{
		return new InterfaceDataType
		{
			Schema = schema,
			Name = GetName(interfaceElement),
			Namespace =  GetNamespace(interfaceElement),
			InterfaceFactory = ParseImplementedInterfaces(interfaceElement, schema),
			IsAbstract = interfaceElement.Attribute(ABSTRACT_ATTRIBUTE_NAME) is {} abstractAttr && bool.Parse(abstractAttr.Value),
			Properties = ParseProperties(interfaceElement, schema, []),
			Metadatas = ParseMetadatas(interfaceElement, schema),
		};
	}

	private Func<IReadOnlyList<IInterfaceDataType>> ParseImplementedInterfaces(XElement compositeTypeElement, ISchema typeSchema)
	{
		string interfaceAttributeName = compositeTypeElement.Name != INTERFACE_ELEMENT_NAME ? IMPLEMENTS_ATTRIBUTE_NAME : EXTENDS_ATTRIBUTE_NAME;
		string[]? interfacePaths = compositeTypeElement.Attribute(interfaceAttributeName)?.Value.Replace(" ", "").Split(',');
		if (interfacePaths is null || interfacePaths.Length == 0)
		{
			return () => [];
		}

		return () => interfacePaths.Select(interfacePath =>
		{
			string[] nodes = interfacePath.Split('.');
			ISchema schema = nodes.Length == 2 ? typeSchema.ImportMap[nodes[0]] : typeSchema;
			string interfaceTypeName = nodes.Last();
			return schema.DataTypes.OfType<IInterfaceDataType>().Single(interfaceType => interfaceType.Name == interfaceTypeName);
		}).ToArray();
	}

	private Func<T?> ParseBaseType<T>(XElement compositeTypeElement, ISchema typeSchema) where T : class, ICompositeDataType
	{
		string? baseTypeName = compositeTypeElement.Attribute(EXTENDS_ATTRIBUTE_NAME)?.Value;
		if (string.IsNullOrWhiteSpace(baseTypeName))
		{
			return () => null;
		}

		return () => typeSchema.DataTypes.OfType<T>().Single(type => type.Name == baseTypeName);
	}

	private IEnumDataType ParseEnum(XElement enumElement, ISchema schema)
	{
		string? underlyingTypeName = enumElement.Attribute(EXTENDS_ATTRIBUTE_NAME)?.Value ?? _options.DefaultEnumUnderlyingTypeName;
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
		=> enumTypeElement.Elements(ENUM_ELEMENT_ELEMENT_NAME).Select(element => ParseEnumElement(element, schema, GetEnumUnderlyingTypeRange(underlyingType))).ToArray();

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
		if (!Int128.TryParse(enumElementElement.Attribute(VALUE_ATTRIBUTE_NAME)?.Value, out var value))
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


