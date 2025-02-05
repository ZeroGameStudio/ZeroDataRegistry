// Copyright Zero Games. All Rights Reserved.

using ZeroGames.DataRegistry.Compiler.Backend;
using ZeroGames.DataRegistry.Compiler.Frontend;

namespace ZeroGames.DataRegistry.Compiler.Server;

public readonly struct CompilerOptions
{
	public required ISchemaSourceResolver SchemaSourceResolver { get; init; }
	public required IReadOnlyDictionary<SchemaSourceForm, ICompilerFrontend> Frontend { get; init; }
	public required ICompilerBackend Backend { get; init; }
	public required IReadOnlySet<IPrimitiveDataType> Primitives { get; init; }
	public required IGenericContainerDataType GenericListType { get; init; }
	public required IGenericContainerDataType GenericSetType { get; init; }
	public required IGenericContainerDataType GenericMapType { get; init; }
	public required IGenericContainerDataType GenericOptionalType { get; init; }
}


