// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler;

public interface IEnumDataType : IUserDefinedDataType
{
	IPrimitiveDataType UnderlyingType { get; }
	IReadOnlyList<IEnumElement> Elements { get; }
}


