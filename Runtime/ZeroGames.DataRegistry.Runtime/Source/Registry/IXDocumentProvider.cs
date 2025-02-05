// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.DataRegistry.Runtime;

public interface IXDocumentProvider
{
	public static IXDocumentProvider Create(XDocument source) => new XDocumentObjectProvider(source);
	public static IXDocumentProvider Create(Stream source) => new XDocumentStreamProvider(source);
	
	public XDocument Document { get; }
}


