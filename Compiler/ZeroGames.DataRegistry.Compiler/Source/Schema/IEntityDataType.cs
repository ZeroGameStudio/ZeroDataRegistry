// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler;

public interface IEntityDataType : ICompositeDataType
{
	new IEntityDataType? BaseType { get; }
	IReadOnlyList<IProperty> PrimaryKey { get; }
}


