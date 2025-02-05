// Copyright Zero Games. All Rights Reserved.

using System.Text.Json;
using ZeroGames.DataRegistry.Compiler.Backend;
using ZeroGames.DataRegistry.Compiler.Backend.CSharp;
using ZeroGames.DataRegistry.Compiler.Frontend;
using ZeroGames.DataRegistry.Compiler.Frontend.Xml;
using ZeroGames.DataRegistry.Compiler.Server;

namespace ZeroGames.DataRegistry.Compiler.Client;

internal sealed class CompilerClient
{

	public CompilerClient(string sources, string outputDir, string configOverride, Action<object> logger)
	{
		_sources = sources.Split(';').Select(source => new SchemaSourceUri(source)).ToHashSet();
		_outputDir = outputDir;
		_configOverride = configOverride;
		_logger = logger;
		
		ParseConfig(out _config);
	}

	public bool Compile() => CompileAsync().Result;

	private void ParseConfig(out CompilerClientConfig config)
	{
		string configPath = !string.IsNullOrWhiteSpace(_configOverride) ? _configOverride : "./config.json";
		if (!File.Exists(configPath))
		{
			throw new FileNotFoundException($"Config file {configPath} not found.");
		}
		
		using FileStream fs = File.OpenRead(configPath);
		config = JsonSerializer.Deserialize<CompilerClientConfig>(fs, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
		});
	}

	private ICompiler CreateServer()
	{
		CompilerOptions options = new()
		{
			SchemaSourceResolver = CreateSchemaSourceResolver(),
			Frontend = CreateFrontend(),
			Backend = CreateBackend(),
			Primitives = CreatePrimitives(),
			GenericListType = CreateGenericContainerDataType(_config.ListTypeName, EContainerType.List),
			GenericSetType = CreateGenericContainerDataType(_config.SetTypeName, EContainerType.Set),
			GenericMapType = CreateGenericContainerDataType(_config.MapTypeName, EContainerType.Map),
			GenericOptionalType = CreateGenericContainerDataType(_config.OptionalTypeName, EContainerType.Optional),
		};
		return ICompiler.Create(options);
	}

	private ISchemaSourceResolver CreateSchemaSourceResolver()
	{
		return new SchemaSourceResolver(_config.SourceDirs);
	}

	private IReadOnlyDictionary<SchemaSourceForm, ICompilerFrontend> CreateFrontend()
	{
		return new Dictionary<SchemaSourceForm, ICompilerFrontend>
		{
			[new("xml")] = new XmlCompilerFrontend(new()
			{
				DefaultEnumUnderlyingTypeName = _config.DefaultEnumUnderlyingTypeName,
			}),
		};
	}

	private ICompilerBackend CreateBackend()
	{
		return new CSharpCompilerBackend(new()
		{
			ImplicitlyUsings = _config.ImplicitlyUsings,
			GeneratesPartialTypes = _config.GeneratesPartialTypes,
		});
	}

	private IReadOnlySet<IPrimitiveDataType> CreatePrimitives()
	{
		HashSet<IPrimitiveDataType> result = 
		[
			CreateIntrinsicPrimitiveDataType(_config.UInt8TypeName, EPrimitiveType.UInt8),
			CreateIntrinsicPrimitiveDataType(_config.UInt16TypeName, EPrimitiveType.UInt16),
			CreateIntrinsicPrimitiveDataType(_config.UInt32TypeName, EPrimitiveType.UInt32),
			CreateIntrinsicPrimitiveDataType(_config.UInt64TypeName, EPrimitiveType.UInt64),
			CreateIntrinsicPrimitiveDataType(_config.Int8TypeName, EPrimitiveType.Int8),
			CreateIntrinsicPrimitiveDataType(_config.Int16TypeName, EPrimitiveType.Int16),
			CreateIntrinsicPrimitiveDataType(_config.Int32TypeName, EPrimitiveType.Int32),
			CreateIntrinsicPrimitiveDataType(_config.Int64TypeName, EPrimitiveType.Int64),
			CreateIntrinsicPrimitiveDataType(_config.FloatTypeName, EPrimitiveType.Float),
			CreateIntrinsicPrimitiveDataType(_config.DoubleTypeName, EPrimitiveType.Double),
			CreateIntrinsicPrimitiveDataType(_config.BoolTypeName, EPrimitiveType.Bool),
			CreateIntrinsicPrimitiveDataType(_config.StringTypeName, EPrimitiveType.String),
		];
		
		// @TODO: Custom types

		return result;
	}

	private IPrimitiveDataType CreateIntrinsicPrimitiveDataType(string name, EPrimitiveType type)
		=> new PrimitiveDataType { Name = name, Namespace = string.Empty, PrimitiveType = type, CanBeKey = true };

	private IPrimitiveDataType CreateCustomPrimitiveDataType(string name, string @namespace, bool canBeKey)
		=> new PrimitiveDataType { Name = name, Namespace = @namespace, PrimitiveType = EPrimitiveType.Custom, CanBeKey = canBeKey };

	private IGenericContainerDataType CreateGenericContainerDataType(string name, EContainerType type)
		=> new GenericContainerDataType { Name = name, ContainerType = type };

	private async Task<bool> CompileAsync()
	{
		try
		{
			if (Directory.Exists(_outputDir))
			{
				if (_config.RequiresOutputDirNotExists)
				{
					throw new InvalidOperationException();
				}
				
				Directory.Delete(_outputDir, true);
			}
			Directory.CreateDirectory(_outputDir);
			
			bool success = true;
			ICompiler server = CreateServer();
			await foreach (var result in server.CompileAsync(_sources))
			{
				_logger($"[{result.ErrorLevel}] {result.Properties["Uri"]} - {result.Message}");
				if (result.ErrorLevel > ECompilationErrorLevel.Warning)
				{
					success = false;
					continue;
				}
				
				Stream dest = result.Dest;
				string outputPath = $"{_outputDir}/{result.Properties["Name"]}.cs";
				await using FileStream fs = File.OpenWrite(outputPath);
				await dest.CopyToAsync(fs);
			}

			return success;
		}
		catch (Exception ex)
		{
			_logger(ex);
			return false;
		}
	}

	private readonly IReadOnlySet<SchemaSourceUri> _sources;
	private readonly string _outputDir;
	private readonly string _configOverride;
	private readonly Action<object> _logger;

	private readonly CompilerClientConfig _config;

}


