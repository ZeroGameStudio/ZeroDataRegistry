// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

public sealed partial class XmlCompilerFrontend : ICompilerFrontend
{

	public XmlCompilerFrontend(XmlCompilerFrontendOptions options)
	{
		_options = options;
	}
	
	public Task<IEnumerable<SchemaSourceUri>> GetUsingSchemasAsync(Stream source)
	{
		XElement root = GetRootElement(source);
		try
		{
			IEnumerable<SchemaSourceUri> result = root.Elements(_usingElementName).Select(e => new SchemaSourceUri(e.Attribute(_uriAttributeName)!.Value)).ToArray();
			return Task.FromResult(result);
		}
		catch (Exception ex)
		{
			throw new ParserException(ex);
		}
	}

	public async Task<ISchema> CompileAsync(SchemaSourceUri uri, Stream source)
	{
		return ParseSchema(uri, GetRootElement(source), await GetUsingSchemasAsync(source));
	}

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
		if (root is null || root.Name != _registryElementName)
		{
			throw new ParserException();
		}
		
		return root;
	}

	private ISchema ParseSchema(SchemaSourceUri uri, XElement root, IEnumerable<SchemaSourceUri> usingSchemas)
	{
		Schema schema = new(schema => ParseDataTypes(root, schema), schema => ParseMetadatas(root, schema))
		{
			Uri = uri,
			Name = GetName(root),
			Namespace = GetNamespace(root),
			UsingSchemas = usingSchemas.Select(us => CompilationContext.GetSchema(us)).ToHashSet(),
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


