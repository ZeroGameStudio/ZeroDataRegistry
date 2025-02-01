// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Core;

public interface IPrimitiveDataType : IDataType
{
	EPrimitiveType PrimitiveType { get; }
	bool CanBeKey { get; }
}


