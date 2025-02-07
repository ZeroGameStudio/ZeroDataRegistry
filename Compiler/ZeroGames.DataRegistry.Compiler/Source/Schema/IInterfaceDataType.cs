// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler;

public interface IInterfaceDataType : ICompositeDataType
{
	IDataType? IUserDefinedDataType.BaseType => null;
	bool ICompositeDataType.IsAbstract => true;
}


