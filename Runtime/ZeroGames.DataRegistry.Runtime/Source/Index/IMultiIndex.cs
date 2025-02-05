// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Runtime;

public interface IMultiIndex<in TKey, TEntity> : IRegistryElement
	where TKey : notnull
	where TEntity : class, IEntity
{
	bool TryGetEntities(TKey key, out ICollection<TEntity> entities);
	TEntity[] this[TKey key] { get; }
}


