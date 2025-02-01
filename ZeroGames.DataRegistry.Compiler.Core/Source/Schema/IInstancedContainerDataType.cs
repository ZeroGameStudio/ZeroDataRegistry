﻿// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Core;

public interface IInstancedContainerDataType : IDataType
{
	IGenericContainerDataType GenericType { get; }
	IPrimitiveDataType KeyType { get; }
	IDataType ValueType { get; }
}


