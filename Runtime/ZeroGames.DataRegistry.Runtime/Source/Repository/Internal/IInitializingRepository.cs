// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Runtime;

internal interface IInitializingRepository : IRepository
{
	void RegisterEntity(IEntity entity);
}


