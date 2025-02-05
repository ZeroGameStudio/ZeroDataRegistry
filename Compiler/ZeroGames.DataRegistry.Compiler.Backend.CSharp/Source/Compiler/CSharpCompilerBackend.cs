// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Backend.CSharp;

public partial class CSharpCompilerBackend : ICompilerBackend
{

	public CSharpCompilerBackend(CSharpCompilerBackendOptions options)
	{
		_options = options;
	}
	
	public async IAsyncEnumerable<CompilationUnitResult> CompileAsync(ISchema schema)
	{
		await foreach (var task in Task.WhenEach(SetupCompilations(schema)))
		{
			yield return task.Result;
		}
	}

	public ICompilationContext CompilationContext { get; private set; } = null!;
	ICompilationContext ICompilationContextReceiver.CompilationContext { set => CompilationContext = value; }

	private Task<CompilationUnitResult>[] SetupCompilations(ISchema schema)
		=> schema.DataTypes.Select(CompileTypeAsync).Append(CompileRegistryAsync(schema)).ToArray();

	private readonly CSharpCompilerBackendOptions _options;

}


