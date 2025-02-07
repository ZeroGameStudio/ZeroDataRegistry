// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Runtime;

public interface IRepository : IRegistryElement
{
	Type EntityType { get; }
	IEnumerable<IEntity> Entities { get; }
}

public interface IRepository<TPrimaryKey, TEntity> : IRepository, IReadOnlyDictionary<TPrimaryKey, TEntity>
	where TPrimaryKey : notnull
	where TEntity : class, IEntity<TPrimaryKey>
{
	Type IRepository.EntityType => typeof(TEntity);
}


