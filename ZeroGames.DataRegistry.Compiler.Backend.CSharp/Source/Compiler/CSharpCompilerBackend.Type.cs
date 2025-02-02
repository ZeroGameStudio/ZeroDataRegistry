// Copyright Zero Games. All Rights Reserved.

using System.Text;
using ZeroGames.DataRegistry.Compiler.Core;

namespace ZeroGames.DataRegistry.Compiler.Backend.CSharp;

public partial class CSharpCompilerBackend
{

	private string GetFullNamespace(IDataType type)
	{
		if (type is IUserDefinedDataType userDefinedDataType)
		{
			string[] nodes = [ userDefinedDataType.Schema.Namespace, userDefinedDataType.Namespace ];
			return string.Join('.', nodes.Where(node => !string.IsNullOrWhiteSpace(node)));
		}

		return type.Namespace;
	}

	private string GetTypeKindCode(IUserDefinedDataType type)
		=> type is IEnumDataType ? "enum" : _options.GeneratesPartialTypes ? "partial class" : "class";

	private string GetBaseTypeCode(IUserDefinedDataType type)
		=> type.BaseType is {} baseType ? $" : {baseType.Name}" : string.Empty;

	private string GetUsingsCode(IUserDefinedDataType type)
	{
		string selfNamespace = GetFullNamespace(type);
		List<string> usings = new();
		if (type.BaseType is { } baseType)
		{
			LootNamespace(baseType, usings);
		}

		if (type is ICompositeDataType compositeType)
		{
			foreach (var property in compositeType.Properties)
			{
				LootNamespace(property.Type, usings);
			}
		}
		
		IEnumerable<string> finalUsings = usings
			.Distinct()
			.Where(us => !string.IsNullOrWhiteSpace(us) && !IsSubnamespaceOf(us, selfNamespace) && !_options.ImplicitlyUsings.Contains(us))
			.Select(us => $"using {us};");
		return string.Join(Environment.NewLine, finalUsings);
	}

	private bool IsSubnamespaceOf(string super, string sub)
	{
		if (super == sub)
		{
			return true;
		}

		if (sub.Length <= super.Length)
		{
			return false;
		}

		if (!sub.StartsWith(super))
		{
			return false;
		}
		
		string remaining = sub.Substring(super.Length);
		return remaining.StartsWith('.');
	}

	private void LootNamespace(IDataType type, List<string> namespaces)
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

	private string GetNamespaceCode(IUserDefinedDataType type)
	{
		string @namespace = GetFullNamespace(type);
		if (string.IsNullOrWhiteSpace(@namespace))
		{
			return string.Empty;
		}
		
		return $"namespace {@namespace};";
	}

	private string GetTypeDefinitionCode(IUserDefinedDataType type)
	{
		return
$@"public {GetTypeKindCode(type)} {type.Name}{GetBaseTypeCode(type)}
{{
{Indent(GetMembersCode(type))}
}}


";
	}

	private string GetMembersCode(IUserDefinedDataType type)
	{
		if (type is ICompositeDataType compositeType)
		{
			return GetPropertiesCode(compositeType);
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

	private string GetPropertiesCode(ICompositeDataType type)
		=> string.Join(Environment.NewLine, type.Properties.Select(property => $"public required {GetTypeNameCode(property.Type)} {property.Name} {{ get; init; }}"));

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
				"// Copyright Zero Games. All Rights Reserved.",
				GetUsingsCode(type),
				GetNamespaceCode(type),
				GetTypeDefinitionCode(type),
			];

			string code = string.Join(Environment.NewLine + Environment.NewLine, codeBlocks.Where(header => !string.IsNullOrWhiteSpace(header)));

			MemoryStream stream = new();
			using (StreamWriter writer = new(stream, Encoding.UTF8, leaveOpen: true))
			{
				writer.Write(code);
			}
			stream.Seek(0, SeekOrigin.Begin);

			Dictionary<string, string> properties = new()
			{
				["Type"] = type is IEntityDataType ? "Entity" : type is IStructDataType ? "Struct" : type is IEnumDataType ? "Enum" : throw new EmitterException(),
				["Uri"] = $"{type.Schema.Name}.{type.Name}",
				["Name"] = type.Name,
				["Namespace"] = GetFullNamespace(type),
				["Language"] = "C#",
			};

			return new CompilationUnitResult(stream, ECompilationErrorLevel.Success, "Compilation success.", properties);
		});
}


