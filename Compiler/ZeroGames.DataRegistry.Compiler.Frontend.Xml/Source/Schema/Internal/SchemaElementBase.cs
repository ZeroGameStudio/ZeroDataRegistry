// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

internal abstract class SchemaElementBase : ISchemaElement
{
	public required ISchema Schema { get; init; }
	public required string Name { get; init; }
	public required IReadOnlyList<IMetadata> Metadatas { get; init; }
}


