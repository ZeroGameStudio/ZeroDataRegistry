// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.DataRegistry.Runtime;

public class RegistryFactory<T> : IRegistryFactory<T> where T : class, IRegistry, new()
{
	
	public T Create(IXDocumentProvider sourceProvider, IReadOnlySet<IRegistry> imports)
	{
		XDocument document = sourceProvider.Document;
		throw new NotImplementedException();
	}
	
	//private static IReadOnlyDictionary<>
	
}


