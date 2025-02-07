// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Runtime;

public interface IEntity
{
	object PrimaryKey { get; }
	bool IsAbstract { get; }
}

public interface IEntity<out TPrimaryKey> : IEntity where TPrimaryKey : notnull
{
	public new TPrimaryKey PrimaryKey { get; }
	object IEntity.PrimaryKey => PrimaryKey;
}


