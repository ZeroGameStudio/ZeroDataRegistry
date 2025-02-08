


using System.Xml.Linq;
using ZeroGames.DataRegistry.Runtime;
using ZeroGames.DataRegistry.Test;
using ZeroGames.DataRegistry.Test.Shared;

var sharedRegistry = new RegistryFactory().Create<SharedRegistry>(IXDocumentProvider.Create(XDocument.Load("sharedconfig.xml")), []);
var mainRegistry = new RegistryFactory().Create<MainRegistry>(IXDocumentProvider.Create(XDocument.Load("mainconfig.xml")), [ sharedRegistry ]);
Console.ReadKey();
Console.WriteLine("exit...");

// schema主键/外键配置方式优化、zdml语法糖提前到源码层而不是运行时、合并、变换、Excel、Validator


