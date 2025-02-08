// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Client;

internal readonly struct CompilerClientConfig
{
	public required List<string> SourceDirs { get; init; }
	
	public required string UInt8TypeName { get; init; }
	public required string UInt16TypeName { get; init; }
	public required string UInt32TypeName { get; init; }
	public required string UInt64TypeName { get; init; }
	public required string Int8TypeName { get; init; }
	public required string Int16TypeName { get; init; }
	public required string Int32TypeName { get; init; }
	public required string Int64TypeName { get; init; }
	public required string FloatTypeName { get; init; }
	public required string DoubleTypeName { get; init; }
	public required string BoolTypeName { get; init; }
	public required string StringTypeName { get; init; }
	
	public required string ListTypeName { get; init; }
	public required string SetTypeName { get; init; }
	public required string MapTypeName { get; init; }
	public required string OptionalTypeName { get; init; }
	
	public string? DefaultPrimaryKey { get; init; }
	public string? DefaultEnumUnderlyingTypeName { get; init; }
	
	public required HashSet<string> ImplicitlyUsings { get; init; }
	
	public required bool RequiresOutputDirNotExists { get; init; }
	public bool GeneratesPartialTypes { get; init; }
}


