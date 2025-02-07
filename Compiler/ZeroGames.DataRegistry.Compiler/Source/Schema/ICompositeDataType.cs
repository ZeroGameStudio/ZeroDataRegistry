// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler;

public interface ICompositeDataType : IUserDefinedDataType
{
	new ICompositeDataType? BaseType { get; }
	bool IsAbstract { get; }
	IReadOnlyList<IInterfaceDataType> Interfaces { get; }
	IReadOnlyList<IProperty> Properties { get; }
	
	IDataType? IUserDefinedDataType.BaseType => BaseType;
}


