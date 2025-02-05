// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.DataRegistry.Runtime;

internal class RepositoryFactory<TPrimaryKey, TEntity>
	where TPrimaryKey : notnull
	where TEntity : class, IEntity, new()
{
	
	public IRepository<TPrimaryKey, TEntity> Create(XElement source, out Action setupDependencies)
	{
		throw new NotImplementedException();
	}
	
}


