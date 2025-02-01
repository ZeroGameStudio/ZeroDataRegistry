// Copyright Zero Games. All Rights Reserved.

using ZeroGames.DataRegistry.Compiler.Core;

namespace ZeroGames.DataRegistry.Compiler.Frontend;

/// <summary>
/// Compiles schema source to ISchema.
/// User can provide custom implementation of this interface to support specific schema source format.
/// </summary>
public interface ICompilerFrontend : ICompilationContextReceiver
{
	Task<IEnumerable<SchemaSourceUri>> GetUsingSchemasAsync(Stream source);
	Task<ISchema> CompileAsync(SchemaSourceUri uri, Stream source);
}


