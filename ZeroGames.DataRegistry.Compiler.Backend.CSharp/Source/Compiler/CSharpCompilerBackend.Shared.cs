// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Backend.CSharp;

public partial class CSharpCompilerBackend
{
	
	private static string Indent(string text)
		=> text.Insert(0, "\t").Replace(Environment.NewLine, Environment.NewLine + '\t');
	
}


