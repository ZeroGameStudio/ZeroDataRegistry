// Copyright Zero Games. All Rights Reserved.

using ZeroGames.DataRegistry.Compiler.Core;

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

internal sealed class EnumElement : SchemaElementBase, IEnumElement
{
	public required Int128 Value { get; init; }
}


