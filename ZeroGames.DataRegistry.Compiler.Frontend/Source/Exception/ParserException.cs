// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Frontend;

public class ParserException(string? message = null, Exception? inner = null) : Exception(message ?? "Failed to compile schema.", inner)
{
	public ParserException(Exception? inner) : this(null, inner){}
}


