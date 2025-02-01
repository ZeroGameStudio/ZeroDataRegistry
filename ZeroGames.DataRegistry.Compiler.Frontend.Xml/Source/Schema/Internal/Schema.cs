// Copyright Zero Games. All Rights Reserved.

using ZeroGames.DataRegistry.Compiler.Core;

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

internal sealed class Schema : ISchema
{
	public Schema(Func<ISchema, IReadOnlyList<IUserDefinedDataType>> typeFactory, Func<ISchema, IReadOnlyList<IMetadata>> metadataFactory)
	{
		_schema = this;
		DataTypes = typeFactory(this);
		Metadatas = metadataFactory(this);
	}

	public required SchemaSourceUri Uri { get; init; }
	ISchema ISchemaElement.Schema => _schema;
	public required string Name { get; init; }
	public required string Namespace { get; init; }
	public required IReadOnlySet<ISchema> UsingSchemas { get; init; }
	public IReadOnlyList<IUserDefinedDataType> DataTypes { get; }
	public IReadOnlyList<IMetadata> Metadatas { get; }

	private readonly ISchema _schema;
}


