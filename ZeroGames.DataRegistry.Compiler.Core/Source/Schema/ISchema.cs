// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Core;

public interface ISchema : ISchemaElement, INamespaceProvider
{
	SchemaSourceUri Uri { get; }
	IReadOnlySet<ISchema> UsingSchemas { get; }
	IReadOnlyList<IUserDefinedDataType> DataTypes { get; }
}


