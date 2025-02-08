


using System.Xml.Linq;
using ZeroGames.DataRegistry.Runtime;
using ZeroGames.DataRegistry.Test;
using ZeroGames.DataRegistry.Test.Shared;

var sharedRegistry = new RegistryFactory().Create<SharedRegistry>(IXDocumentProvider.Create(XDocument.Load("sharedconfig.xml")), []);
var mainRegistry = new RegistryFactory().Create<MainRegistry>(IXDocumentProvider.Create(XDocument.Load("mainconfig.xml")), [ sharedRegistry ]);
Console.ReadKey();
Console.WriteLine("exit...");


