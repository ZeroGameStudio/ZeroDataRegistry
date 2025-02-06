// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Runtime;

public interface IRegistryFactory
{
	T Create<T>(IXDocumentProvider sourceProvider, IEnumerable<IRegistry> imports) where T : class, IRegistry;
}


