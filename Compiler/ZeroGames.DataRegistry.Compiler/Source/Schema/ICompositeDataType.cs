// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler;

public interface ICompositeDataType : IUserDefinedDataType
{
	bool IsAbstract { get; }
	IReadOnlyList<IProperty> Properties { get; }
}


