// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.DataRegistry.Runtime;

internal class XDocumentStreamProvider : IXDocumentProvider
{
	public XDocumentStreamProvider(Stream source)
	{
		Document = XDocument.Load(source);
		source.Seek(0, SeekOrigin.Begin);
	}
	
	public XDocument Document { get; }
}


