// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

internal abstract class DataTypeBase : IDataType
{
	public required string Name { get; init; }
	public required string Namespace { get; init; }
}


