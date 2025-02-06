// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Runtime;

public interface IEntity
{
	
}

public interface IEntity<out TPrimaryKey> : IEntity
{
	public TPrimaryKey PrimaryKey { get; }
}


