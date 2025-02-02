// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler;

public interface IGenericContainerDataType : IDataType
{
	IInstancedContainerDataType Instantiate(IPrimitiveDataType keyType, IDataType valueType);
	
	EContainerType ContainerType { get; }
}


