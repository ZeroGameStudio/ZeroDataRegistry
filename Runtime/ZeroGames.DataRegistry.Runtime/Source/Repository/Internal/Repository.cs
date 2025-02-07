// Copyright Zero Games. All Rights Reserved.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace ZeroGames.DataRegistry.Runtime;

internal class Repository<TPrimaryKey, TEntity> : IRepository<TPrimaryKey, TEntity>, IInitializingRepository, INotifyInitialization
	where TPrimaryKey : notnull
	where TEntity : class, IEntity<TPrimaryKey>
{
	
	void INotifyInitialization.PreInitialize()
	{
		_state = EState.Initializing;
	}

	void INotifyInitialization.PostInitialize()
	{
		_abstractStorage = null;
		
		_state = EState.Initialized;
	}

	void IDisposable.Dispose()
	{
		_state = EState.Disposed;
		_storage.Clear();
	}

	public Dictionary<TPrimaryKey, TEntity>.Enumerator GetEnumerator()
	{
		GuardInvariant();
		return _storage.GetEnumerator();
	}
	IEnumerator<KeyValuePair<TPrimaryKey, TEntity>> IEnumerable<KeyValuePair<TPrimaryKey, TEntity>>.GetEnumerator() => GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public bool ContainsKey(TPrimaryKey key)
	{
		GuardInvariant();
		return _storage.ContainsKey(key);
	}

	public bool TryGetValue(TPrimaryKey key, [MaybeNullWhen(false)] out TEntity value)
	{
		GuardInvariant();
		return _storage.TryGetValue(key, out value);
	}

	void IInitializingRepository.RegisterEntity(IEntity entity, bool @abstract)
	{
		if (_state != EState.Initializing)
		{
			throw new InvalidOperationException("Repository is immutable unless it is initializing.");
		}
		
		var primaryKey = (TPrimaryKey)entity.PrimaryKey;

		if (@abstract)
		{
			_abstractStorage ??= [];
		}

		Dictionary<TPrimaryKey, TEntity> storage = @abstract ? _abstractStorage! : _storage;
		if (storage.ContainsKey(primaryKey))
		{
			throw new InvalidOperationException($"Primary key '{primaryKey}' already exists.");
		}
		
		storage[primaryKey] = (TEntity)entity;
	}

	bool IInitializingRepository.TryGetEntity(object primaryKey, bool evenIfAbstract, [NotNullWhen(true)] out IEntity? entity)
	{
		var typedPrimaryKey = (TPrimaryKey)primaryKey;
		bool suc = _storage.TryGetValue(typedPrimaryKey, out var typedEntity);
		if (!suc && evenIfAbstract && _abstractStorage is not null)
		{
			suc = _abstractStorage.TryGetValue(typedPrimaryKey, out typedEntity);
		}
		
		entity = typedEntity;
		return suc;
	}

	public TEntity this[TPrimaryKey key]
	{
		get
		{
			GuardInvariant();
			return _storage[key];
		}
	}

	public int32 Count
	{
		get
		{
			GuardInvariant();
			return _storage.Count;
		}
	}

	public IEnumerable<TPrimaryKey> Keys
	{
		get
		{
			GuardInvariant();
			return _storage.Keys;
		}
	}

	public IEnumerable<TEntity> Values
	{
		get
		{
			GuardInvariant();
			return _storage.Values;
		}
	}

	IEnumerable<IEntity> IRepository.Entities => _storage.Values;
	
	public required IRegistry Registry { get; init; }
	public required string Name { get; init; }
	public bool IsDisposed => _state == EState.Disposed;

	private enum EState : uint8
	{
		Uninitialized,
		Initializing,
		Initialized,
		Disposed,
	}

	private void GuardInvariant()
	{
		if (_state < EState.Initialized)
		{
			throw new InvalidOperationException("Repository is not fully initialized yet.");
		}
		
		RegistryElementExtensions.GuardInvariant(this);
	}

	private EState _state = EState.Uninitialized;

	private readonly Dictionary<TPrimaryKey, TEntity> _storage = new();
	private Dictionary<TPrimaryKey, TEntity>? _abstractStorage;

}


