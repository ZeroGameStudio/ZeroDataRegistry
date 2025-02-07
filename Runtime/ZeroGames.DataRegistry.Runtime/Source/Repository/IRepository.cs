// Copyright Zero Games. All Rights Reserved.

using System.Diagnostics.CodeAnalysis;

namespace ZeroGames.DataRegistry.Runtime;

public interface IRepository : IRegistryElement
{
	bool TryGetEntity(object primaryKey, [NotNullWhen(true)] out IEntity? entity);
	Type EntityType { get; }
	IEnumerable<IEntity> Entities { get; }
}

public interface IRepository<TPrimaryKey, TEntity> : IRepository, IReadOnlyDictionary<TPrimaryKey, TEntity>
	where TPrimaryKey : notnull
	where TEntity : class, IEntity<TPrimaryKey>
{
	Type IRepository.EntityType => typeof(TEntity);
}


