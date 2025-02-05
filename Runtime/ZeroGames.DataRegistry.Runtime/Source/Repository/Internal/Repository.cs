// Copyright Zero Games. All Rights Reserved.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace ZeroGames.DataRegistry.Runtime;

internal class Repository<TPrimaryKey, TEntity> : IRepository<TPrimaryKey, TEntity>
	where TPrimaryKey : notnull
	where TEntity : class, IEntity
{

	public Repository(IRegistry registry, string name)
	{
		Registry = registry;
		Name = name;
	}

	void IDisposable.Dispose()
	{
		IsDisposed = true;
		_storage.Clear();
	}

	public Dictionary<TPrimaryKey, TEntity>.Enumerator GetEnumerator()
	{
		this.GuardInvariant();
		return _storage.GetEnumerator();
	}
	IEnumerator<KeyValuePair<TPrimaryKey, TEntity>> IEnumerable<KeyValuePair<TPrimaryKey, TEntity>>.GetEnumerator() => GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public bool ContainsKey(TPrimaryKey key)
	{
		this.GuardInvariant();
		return _storage.ContainsKey(key);
	}

	public bool TryGetValue(TPrimaryKey key, [MaybeNullWhen(false)] out TEntity value)
	{
		this.GuardInvariant();
		return _storage.TryGetValue(key, out value);
	}

	public TEntity this[TPrimaryKey key]
	{
		get
		{
			this.GuardInvariant();
			return _storage[key];
		}
	}

	public int32 Count
	{
		get
		{
			this.GuardInvariant();
			return _storage.Count;
		}
	}

	public IEnumerable<TPrimaryKey> Keys
	{
		get
		{
			this.GuardInvariant();
			return _storage.Keys;
		}
	}

	public IEnumerable<TEntity> Values
	{
		get
		{
			this.GuardInvariant();
			return _storage.Values;
		}
	}

	public IRegistry Registry { get; }
	public string Name { get; }
	public bool IsDisposed { get; private set; }

	private readonly Dictionary<TPrimaryKey, TEntity> _storage = new();
	
}


