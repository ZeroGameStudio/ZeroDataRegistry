// Copyright Zero Games. All Rights Reserved.

using System.Text;

namespace ZeroGames.DataRegistry.Compiler.Backend.CSharp;

public partial class CSharpCompilerBackend
{

	private string GetRegistryUsingCode(ISchema schema)
	{
		List<string> usings = [];
		foreach (var import in schema.ImportMap.Values.Where(HasEntity))
		{
			usings.Add(GetFullNamespace(import));
		}

		foreach (var type in schema.DataTypes.Where(t => t is IEntityDataType))
		{
			usings.Add(GetFullNamespace(type));
		}

		return GetDistinctUsingsCode(usings, schema.Namespace, true);
	}

	private string GetRegistryDefinitionCode(ISchema schema)
	{
		return
$@"{GetGeneratedCodeAttributeCode()}
public partial class {schema.Name} : IRegistry
{{
{Indent(GetRegistryMembersCode(schema))}
}}";
	}

	private string GetRegistryMembersCode(ISchema schema)
	{
		string[] codeBlocks =
		[
			GetRegistryImportsCode(schema),
			GetRegistryRepositoriesCode(schema),
			GetRegistryInterfaceImplementationsCode(schema),
		];

		return string.Join(Environment.NewLine + Environment.NewLine, codeBlocks.Where(block => !string.IsNullOrWhiteSpace(block)));
	}

	private string GetRegistryImportsCode(ISchema schema)
		=> string.Join(Environment.NewLine + Environment.NewLine, schema.ImportMap.Where(import => HasEntity(import.Value)).Select(import
			=> $"[Import]{Environment.NewLine}public required {import.Value.Name} {import.Key} {{ get {{ this.GuardInvariant(); return field; }} init; }}"));

	private string GetRegistryRepositoriesCode(ISchema schema)
		=> string.Join(Environment.NewLine + Environment.NewLine, schema.DataTypes.OfType<IEntityDataType>().Select(type
			=> $"[Repository]{Environment.NewLine}public required IRepository<{GetTypePrimaryKeyTypeCode(type)}, {type.Name}> {type.Name}Repository {{ get {{ this.GuardInvariant(); return field; }} init; }}"));

	private string GetRegistryInterfaceImplementationsCode(ISchema schema)
	{
		return
$@"void IDisposable.Dispose() => IsDisposed = true;
IRegistry IRegistryElement.Registry => this;
public string Name => nameof({schema.Name});
public bool IsDisposed {{ get; private set; }}";
	}

	private Task<CompilationUnitResult> CompileRegistryAsync(ISchema schema)
		=> Task.Run(() =>
		{
			string[] codeBlocks =
			[
				GetHeaderCode(),
				GetRegistryUsingCode(schema),
				GetNamespaceCode(schema),
				GetRegistryDefinitionCode(schema),
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
				["Type"] = "Registry",
				["Schema"] = schema.Name,
				["Uri"] = schema.Uri.Address,
				["Name"] = schema.Name,
				["Namespace"] = schema.Namespace,
				["Language"] = "C#",
			};

			return new CompilationUnitResult(stream, ECompilationErrorLevel.Success, "Compilation success.", properties);
		});
}


