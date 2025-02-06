// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Runtime;

internal interface IDynamicRepository
{
	void RegisterEntity(object primaryKey, object entity);
}


