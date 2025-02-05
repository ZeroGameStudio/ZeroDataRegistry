// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Client;

public class InstancedContainerDataType : IInstancedContainerDataType
{
	public required string Name { get; init; }
	public string Namespace => string.Empty;
	public required IGenericContainerDataType GenericType { get; init; }
	public required IPrimitiveDataType KeyType { get; init; }
	public required IDataType ValueType { get; init; }
}


