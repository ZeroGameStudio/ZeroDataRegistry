// Copyright Zero Games. All Rights Reserved.

using System.Diagnostics.CodeAnalysis;

namespace ZeroGames.DataRegistry.Runtime;

internal interface IInitializingRepository : IRepository
{
	void RegisterEntity(IEntity entity, bool @abstract);
	bool TryGetEntity(object primaryKey, bool evenIfAbstract, [NotNullWhen(true)] out IEntity? entity);
}


