// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Backend.CSharp;

public partial class CSharpCompilerBackend
{

	private Task<CompilationUnitResult> CompileRegistryAsync(ISchema schema)
		=> Task.Run(() =>
		{
			Dictionary<string, string> properties = new()
			{
				["Type"] = "Registry",
				["Uri"] = schema.Uri.Address,
				["Name"] = schema.Name,
				["Namespace"] = schema.Namespace,
				["Language"] = "C#",
			};

			return Task.FromResult(new CompilationUnitResult(new MemoryStream(), ECompilationErrorLevel.Success, "Compilation success.", properties));
		});
}


