// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler;

public class CompilationException(string? message = null, Exception? inner = null) : Exception(message ?? "Failed to compile.", inner)
{
	public CompilationException(Exception? inner) : this(null, inner){}
}


