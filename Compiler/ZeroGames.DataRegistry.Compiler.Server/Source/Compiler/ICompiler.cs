// Copyright Zero Games. All Rights Reserved.

using ZeroGames.DataRegistry.Compiler.Backend;

namespace ZeroGames.DataRegistry.Compiler.Server;

public interface ICompiler
{
	public static ICompiler Create(in CompilerOptions options) => new Compiler(options);
	
	IAsyncEnumerable<CompilationUnitResult> CompileAsync(IReadOnlySet<SchemaSourceUri> sources);
}


