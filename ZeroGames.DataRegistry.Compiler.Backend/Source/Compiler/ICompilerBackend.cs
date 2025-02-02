// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Backend;

/// <summary>
/// Compiles ISchema to common programming language source code defining data structures in the schema.
/// User can provide custom implementation of this interface to support specific language.
/// </summary>
public interface ICompilerBackend : ICompilationContextReceiver
{
	IAsyncEnumerable<CompilationUnitResult> CompileAsync(ISchema schema);
}


