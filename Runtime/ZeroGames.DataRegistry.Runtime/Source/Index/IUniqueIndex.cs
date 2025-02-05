// Copyright Zero Games. All Rights Reserved.

using System.Diagnostics.CodeAnalysis;

namespace ZeroGames.DataRegistry.Runtime;

public interface IUniqueIndex<in TKey, TEntity> : IRegistryElement
	where TKey : notnull
	where TEntity : class
{
	bool TryGetEntity(TKey key, [NotNullWhen(true)] out TEntity? entity);
	TEntity this[TKey key] { get; }
}


