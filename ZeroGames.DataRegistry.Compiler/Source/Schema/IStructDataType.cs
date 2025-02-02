// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler;

public interface IStructDataType : ICompositeDataType
{
	new IStructDataType? BaseType { get; }
}


