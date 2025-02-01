// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Core;

public interface IEnumDataType : IUserDefinedDataType
{
	IPrimitiveDataType UnderlyingType { get; }
	IReadOnlyList<IEnumElement> Elements { get; }
}


