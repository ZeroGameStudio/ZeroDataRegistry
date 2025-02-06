// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

public sealed partial class XmlCompilerFrontend : ICompilerFrontend
{

	public XmlCompilerFrontend(XmlCompilerFrontendOptions options)
	{
		_options = options;
	}
	
	public Task<IEnumerable<SchemaSourceUri>> GetImportsAsync(Stream source)
	{
		XElement root = GetRootElement(source);
		try
		{
			IEnumerable<SchemaSourceUri> result = root
				.Elements(_importElementName)
				.Select(e => new SchemaSourceUri(e.Attribute(_uriAttributeName)!.Value))
				.ToArray();
			
			return Task.FromResult(result);
		}
		catch (Exception ex)
		{
			throw new ParserException(ex);
		}
	}

	public Task<ISchema> CompileAsync(SchemaSourceUri uri, Stream source)
		=> Task.FromResult(ParseSchema(uri, GetRootElement(source)));

	public ICompilationContext CompilationContext { get; private set; } = null!;
	ICompilationContext ICompilationContextReceiver.CompilationContext { set => CompilationContext = value; }

	private XElement GetRootElement(Stream source)
	{
		XDocument doc;
		try
		{
			doc = XDocument.Load(source);
			source.Seek(0, SeekOrigin.Begin);
		}
		catch (Exception ex)
		{
			throw new ParserException(ex);
		}

		XElement? root = doc.Root;
		if (root is null || root.Name != _schemaElementName)
		{
			throw new ParserException();
		}
		
		return root;
	}

	private ISchema ParseSchema(SchemaSourceUri uri, XElement root)
	{
		IEnumerable<(SchemaSourceUri Uri, string Alias)> imports = root
			.Elements(_importElementName)
			.Select(e =>
			{
				SchemaSourceUri import = new(e.Attribute(_uriAttributeName)!.Value);
				return (import, e.Attribute(_aliasAttributeName)?.Value ?? CompilationContext.GetSchema(import).Name);
			})
			.ToArray();
		
		Schema schema = new(imports.ToDictionary(import => import.Alias, import => CompilationContext.GetSchema(import.Uri)), schema => ParseDataTypes(root, schema), schema => ParseMetadatas(root, schema))
		{
			Uri = uri,
			Name = GetName(root),
			Namespace = GetNamespace(root),
		};

		foreach (var type in schema.DataTypes.OfType<ICompositeDataType>().Select(x => (ISetupDependenciesSource)x))
		{
			if (!type.SetupDependencies())
			{
				throw new ParserException();
			}
			
			foreach (var prop in ((ICompositeDataType)type).Properties.Select(x => (ISetupDependenciesSource)x))
			{
				if (!prop.SetupDependencies())
				{
					throw new ParserException();
				}
			}
		}
		
		return schema;
	}

	private readonly XmlCompilerFrontendOptions _options;

}


