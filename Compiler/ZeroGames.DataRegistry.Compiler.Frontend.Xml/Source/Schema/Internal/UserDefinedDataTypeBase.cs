// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

internal abstract class UserDefinedDataTypeBase : DataTypeBase, IUserDefinedDataType
{
	public required ISchema Schema { get; init; }
	IDataType? IUserDefinedDataType.BaseType => InternalBaseType;
	public required IReadOnlyList<IMetadata> Metadatas { get; init; }
	
	protected abstract IDataType? InternalBaseType { get; }
}


