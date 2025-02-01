// Copyright Zero Games. All Rights Reserved.

using ZeroGames.DataRegistry.Compiler.Client;

string sources = string.Empty;
string outputDir = string.Empty;
string configOverride = string.Empty;
foreach (var arg in args)
{
	string[] kv = arg.Split('=');
	string name = kv[0].ToLower();
	string value = kv.Length > 1 ? kv[1] : string.Empty;
	switch (name)
	{
		case "sources":
		{
			sources = value;
			break;
		}
		case "outputdir":
		{
			outputDir = value;
			break;
		}
		case "configoverride":
		{
			configOverride = value;
			break;
		}
		default:
		{
			throw new ArgumentOutOfRangeException($"Unknown argument: {name}");
		}
	}
}

if (string.IsNullOrWhiteSpace(sources))
{
	throw new ArgumentOutOfRangeException(nameof(sources));
}

if (string.IsNullOrWhiteSpace(outputDir))
{
	throw new ArgumentOutOfRangeException(nameof(outputDir));
}

Action<object> logger = Console.WriteLine;

return new CompilerClient(sources, outputDir, configOverride, logger).Compile() ? 0 : -1;


