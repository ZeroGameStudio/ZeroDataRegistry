// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Backend.CSharp;

public readonly struct CSharpCompilerBackendOptions
{
	public IReadOnlySet<string> ImplicitlyUsings { get; init; }
	public bool GeneratesPartialTypes { get; init; }
}


