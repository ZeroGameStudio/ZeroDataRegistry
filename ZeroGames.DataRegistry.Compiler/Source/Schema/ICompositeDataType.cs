// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler;

public interface ICompositeDataType : IUserDefinedDataType
{
	IReadOnlyList<IProperty> Properties { get; }
}


