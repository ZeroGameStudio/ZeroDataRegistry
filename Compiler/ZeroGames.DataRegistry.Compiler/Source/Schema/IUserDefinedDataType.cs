// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler;

public interface IUserDefinedDataType : IDataType, ISchemaElement
{
	IDataType? BaseType { get; }
}


