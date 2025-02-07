// Copyright Zero Games. All Rights Reserved.

using System.Text;

namespace ZeroGames.DataRegistry.Compiler.Backend.CSharp;

public partial class CSharpCompilerBackend
{

	private string GetAbstractModifierCode(IUserDefinedDataType type)
		=> type is ICompositeDataType { IsAbstract: true } ? "abstract " : string.Empty;

	private string GetTypeKindCode(IUserDefinedDataType type)
		=> type is IEnumDataType ? "enum" : _options.GeneratesPartialTypes ? "partial class" : "class";

	private string GetBaseTypeCode(IUserDefinedDataType type)
	{
		List<string> baseTypes = [];
		if (type.BaseType is { } baseType)
		{
			baseTypes.Add(baseType.Name);
		}

		if (type is IEntityDataType { BaseType: null } entityType)
		{
			if (entityType.PrimaryKeyComponents.Count > 7)
			{
				throw new NotSupportedException("Primary key more than 7-dimension is not supported.");
			}

			baseTypes.Add($"IEntity<{GetTypePrimaryKeyTypeCode(entityType)}>");
		}
		else if (type is IStructDataType { BaseType: null })
		{
			baseTypes.Add("IStruct");
		}

		return baseTypes.Count > 0 ? $" : {string.Join(", ", baseTypes)}" : string.Empty;
	}

	private string GetTypeUsingsCode(IUserDefinedDataType type)
	{
		List<string> usings = [];
		if (type.BaseType is { } baseType)
		{
			LootTypeNamespace(baseType, usings);
		}

		if (type is ICompositeDataType compositeType)
		{
			foreach (var property in compositeType.Properties)
			{
				LootTypeNamespace(property.Type, usings);
			}
		}

		return GetDistinctUsingsCode(usings, GetFullNamespace(type));
	}

	private void LootTypeNamespace(IDataType type, List<string> namespaces)
	{
		if (type is IInstancedContainerDataType containerType)
		{
			namespaces.Add("System.Collections.Generic");
			IGenericContainerDataType genericContainerType = containerType.GenericType;
			if (genericContainerType == CompilationContext.GenericListType)
			{
				namespaces.Add(GetFullNamespace(containerType.ValueType));
			}
			else if (genericContainerType == CompilationContext.GenericSetType)
			{
				namespaces.Add(GetFullNamespace(containerType.ValueType));
			}
			else if (genericContainerType == CompilationContext.GenericMapType)
			{
				namespaces.Add(GetFullNamespace(containerType.KeyType));
				namespaces.Add(GetFullNamespace(containerType.ValueType));
			}
			else if (genericContainerType == CompilationContext.GenericOptionalType)
			{
				namespaces.Add(GetFullNamespace(containerType.ValueType));
			}
			else
			{
				throw new EmitterException();
			}
		}

		namespaces.Add(GetFullNamespace(type));
	}

	private string GetTypeDefinitionCode(IUserDefinedDataType type)
	{
		return
$@"{GetSchemaAttributeCode(type.Schema)}
{GetGeneratedCodeAttributeCode()}
public {GetAbstractModifierCode(type)}{GetTypeKindCode(type)} {type.Name}{GetBaseTypeCode(type)}
{{
{Indent(GetTypeMembersCode(type))}
}}";
	}

	private string GetTypeMembersCode(IUserDefinedDataType type)
	{
		if (type is IEntityDataType { BaseType: null } entityType)
		{
			string primaryKeyValueCode = entityType.PrimaryKeyComponents.Count == 1
				? entityType.PrimaryKeyComponents[0].Name
				: $"({string.Join(", ", entityType.PrimaryKeyComponents.Select(component => component.Name))})";
			string primaryKeyImplementation = $"public {GetTypePrimaryKeyTypeCode(entityType)} PrimaryKey => {primaryKeyValueCode};";
			return string.Join(Environment.NewLine + Environment.NewLine, primaryKeyImplementation, GetTypePropertiesCode(entityType));
		}
		else if (type is ICompositeDataType compositeType)
		{
			return GetTypePropertiesCode(compositeType);
		}
		else if (type is IEnumDataType enumType)
		{
			return GetEnumElementsCode(enumType);
		}
		else
		{
			throw new EmitterException();
		}
	}

	private string GetTypePropertiesCode(ICompositeDataType type)
		=> string.Join(Environment.NewLine + Environment.NewLine, type.Properties.Select(property =>
		{
			if (property.Name == "PrimaryKey")
			{
				throw new NotSupportedException("Property name 'PrimaryKey' is reserved.");
			}
			
			string attributeCode = property.Role == EPropertyRole.PrimaryKey ? "[PrimaryKey]" : "[Property]";
			return $"{attributeCode}{Environment.NewLine}public required {GetTypeNameCode(property.Type)} {property.Name} {{ get; init; }}";
		}));

	private string GetTypeNameCode(IDataType type)
	{
		if (type is IInstancedContainerDataType containerType)
		{
			IGenericContainerDataType genericContainerType = containerType.GenericType;
			if (genericContainerType == CompilationContext.GenericListType)
			{
				return $"IReadOnlyList<{containerType.ValueType.Name}>";
			}
			else if (genericContainerType == CompilationContext.GenericSetType)
			{
				return $"IReadOnlySet<{containerType.ValueType.Name}>";
			}
			else if (genericContainerType == CompilationContext.GenericMapType)
			{
				return $"IReadOnlyDictionary<{containerType.KeyType.Name}, {containerType.ValueType.Name}>";
			}
			else if (genericContainerType == CompilationContext.GenericOptionalType)
			{
				return $"{containerType.ValueType.Name}?";
			}
			else
			{
				throw new EmitterException();
			}
		}

		return type.Name;
	}

	private string GetEnumElementsCode(IEnumDataType type)
		=> string.Join($",{Environment.NewLine}", type.Elements.Select(element => $"{element.Name} = {element.Value}"));

	private Task<CompilationUnitResult> CompileTypeAsync(IUserDefinedDataType type)
		=> Task.Run(() =>
		{
			string[] codeBlocks =
			[
				GetHeaderCode(),
				GetTypeUsingsCode(type),
				GetNamespaceCode(type),
				GetTypeDefinitionCode(type),
				GetTailCode(),
			];

			string code = string.Join(Environment.NewLine + Environment.NewLine, codeBlocks.Where(block => !string.IsNullOrWhiteSpace(block)));

			MemoryStream stream = new();
			using (StreamWriter writer = new(stream, Encoding.UTF8, leaveOpen: true))
			{
				writer.Write(code);
			}
			stream.Seek(0, SeekOrigin.Begin);

			Dictionary<string, string> properties = new()
			{
				["Type"] = type is IEntityDataType ? "Entity" : type is IStructDataType ? "Struct" : type is IEnumDataType ? "Enum" : throw new EmitterException(),
				["Schema"] = type.Schema.Name,
				["Uri"] = $"{type.Schema.Name}.{type.Name}",
				["Name"] = type.Name,
				["Namespace"] = GetFullNamespace(type),
				["Language"] = "C#",
			};

			return new CompilationUnitResult(stream, ECompilationErrorLevel.Success, "Compilation success.", properties);
		});
	
}


