// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Core;

public sealed class VoidDataType : IPrimitiveDataType
{
	public string Name => "void";
	public string Namespace => "System";
	public EPrimitiveType PrimitiveType => EPrimitiveType.Void;
	public bool CanBeKey => false;
}


