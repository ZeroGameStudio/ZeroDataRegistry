// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Runtime;

public interface IRepository<TPrimaryKey, TEntity> : IRegistryElement, IReadOnlyDictionary<TPrimaryKey, TEntity>
	where TPrimaryKey : notnull
	where TEntity : class, IEntity<TPrimaryKey>
{

}


