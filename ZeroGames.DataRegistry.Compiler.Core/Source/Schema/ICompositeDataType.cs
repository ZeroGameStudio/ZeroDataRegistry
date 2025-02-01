// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Core;

public interface ICompositeDataType : IUserDefinedDataType
{
	IReadOnlyList<IProperty> Properties { get; }
}


