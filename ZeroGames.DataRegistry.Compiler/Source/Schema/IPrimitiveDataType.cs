// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler;

public interface IPrimitiveDataType : IDataType
{
	EPrimitiveType PrimitiveType { get; }
	bool CanBeKey { get; }
}


