// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Core;

public interface IStructDataType : ICompositeDataType
{
	new IStructDataType? BaseType { get; }
}


