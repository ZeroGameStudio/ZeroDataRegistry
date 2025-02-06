// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

internal sealed class Schema : ISchema
{
	public Schema(IReadOnlyDictionary<string, ISchema> importMap, Func<ISchema, IReadOnlyList<IUserDefinedDataType>> typeFactory, Func<ISchema, IReadOnlyList<IMetadata>> metadataFactory)
	{
		ImportMap = importMap;
		DataTypes = typeFactory(this);
		Metadatas = metadataFactory(this);
	}

	public required SchemaSourceUri Uri { get; init; }
	ISchema ISchemaElement.Schema => this;
	public required string Name { get; init; }
	public required string Namespace { get; init; }
	public IReadOnlyDictionary<string, ISchema> ImportMap { get; }
	public IReadOnlyList<IUserDefinedDataType> DataTypes { get; }
	public IReadOnlyList<IMetadata> Metadatas { get; }
}


