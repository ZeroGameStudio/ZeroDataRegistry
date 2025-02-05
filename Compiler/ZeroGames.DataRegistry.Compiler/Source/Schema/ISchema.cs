// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler;

public interface ISchema : ISchemaElement, INamespaceProvider
{
	SchemaSourceUri Uri { get; }
	IReadOnlySet<ISchema> Imports { get; }
	IReadOnlyList<IUserDefinedDataType> DataTypes { get; }
}


