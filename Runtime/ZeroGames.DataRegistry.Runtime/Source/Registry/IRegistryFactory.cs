// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Runtime;

public interface IRegistryFactory<out T> where T : class, IRegistry, new()
{
	T Create(IXDocumentProvider sourceProvider, IEnumerable<IRegistry> imports);
}


