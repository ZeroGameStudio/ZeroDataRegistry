// Copyright Zero Games. All Rights Reserved.

using System.Diagnostics.CodeAnalysis;

namespace ZeroGames.DataRegistry.Compiler.Client;

public class SchemaSourceResolver : ISchemaSourceResolver
{

	public SchemaSourceResolver(IReadOnlyList<string> sourceDirs)
	{
		_sourceDirs = sourceDirs;
	}
	
	public SchemaSourceResolveResult Resolve(SchemaSourceUri uri)
	{
		Stream? source = null;
		foreach (var sourceDir in _sourceDirs)
		{
			if (!Directory.Exists(sourceDir))
			{
				continue;
			}
			
			if (TryReadSource(sourceDir, uri.Address, out source))
			{
				break;
			}
		}

		if (source is null)
		{
			throw new FileNotFoundException();
		}

		return new(source, new("xml"));
	}

	private bool TryReadSource(string directory, string sourceName, [NotNullWhen(true)] out Stream? stream)
	{
		stream = null;
		
		string? sourceFile = Directory.EnumerateFiles(directory, "*.xml", SearchOption.AllDirectories)
			.FirstOrDefault(path => Path.GetFileNameWithoutExtension(path) == sourceName);
		if (string.IsNullOrWhiteSpace(sourceFile))
		{
			return false;
		}

		stream = new MemoryStream(File.ReadAllBytes(sourceFile));
		stream.Seek(0, SeekOrigin.Begin);
		return true;
	}
	
	private readonly IReadOnlyList<string> _sourceDirs;
	
}


