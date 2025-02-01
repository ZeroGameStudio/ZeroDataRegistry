// Copyright Zero Games. All Rights Reserved.

using ZeroGames.DataRegistry.Compiler.Core;

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

internal sealed class EnumDataType : UserDefinedDataTypeBase, IEnumDataType
{
	public required IPrimitiveDataType UnderlyingType { get; init; }
	public required IReadOnlyList<IEnumElement> Elements { get; init; }

	protected override IPrimitiveDataType InternalBaseType => UnderlyingType;
}


