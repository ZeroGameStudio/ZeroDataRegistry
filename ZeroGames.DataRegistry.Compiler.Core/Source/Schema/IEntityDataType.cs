// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Core;

public interface IEntityDataType : ICompositeDataType
{
	new IEntityDataType? BaseType { get; }
	IReadOnlyList<IProperty> PrimaryKey { get; }
}


