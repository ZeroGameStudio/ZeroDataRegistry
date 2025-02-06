// Copyright Zero Games. All Rights Reserved.

using System.Text;

namespace ZeroGames.DataRegistry.Compiler.Backend.CSharp;

public partial class CSharpCompilerBackend
{
	
	private string GetSchemaUsingCode(ISchema schema)
	{
		List<string> usings = [];
		foreach (var type in schema.DataTypes)
		{
			usings.Add(GetFullNamespace(type));
		}

		return GetDistinctUsingsCode(usings, schema.Namespace);
	}

	private string GetSchemaDefinitionCode(ISchema schema)
	{
		return
@$"{GetSchemaAttributeCode(schema)}
{GetDataTypesAttributeCode(schema)}
{GetGeneratedCodeAttributeCode()}
internal static class {GetSchemaTypeName(schema)};";
	}

	private string GetDataTypesAttributeCode(ISchema schema)
	{
		string parameters = schema.DataTypes.Count > 0 ? $"({string.Join(", ", schema.DataTypes.Select(type => $"typeof({type.Name})"))})" : string.Empty;
		return $"[DataTypes{parameters}]";
	}

	private Task<CompilationUnitResult> CompileSchemaAsync(ISchema schema)
		=> Task.Run(() =>
		{
			string[] codeBlocks =
			[
				GetHeaderCode(),
				GetSchemaUsingCode(schema),
				GetNamespaceCode(schema),
				GetSchemaDefinitionCode(schema),
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
				["Type"] = "Schema",
				["Schema"] = schema.Name,
				["Uri"] = schema.Uri.Address,
				["Name"] = GetSchemaTypeName(schema),
				["Namespace"] = schema.Namespace,
				["Language"] = "C#",
			};

			return new CompilationUnitResult(stream, ECompilationErrorLevel.Success, "Compilation success.", properties);
		});
	
}


