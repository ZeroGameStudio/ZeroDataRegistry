// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Core;

public interface ISchemaElement : INameProvider
{
	ISchema Schema { get; }
	IReadOnlyList<IMetadata> Metadatas { get; }
}


