// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Core;

public interface IUserDefinedDataType : IDataType, ISchemaElement
{
	IDataType? BaseType { get; }
}


