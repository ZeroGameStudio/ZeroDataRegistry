// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Client;

public class GenericContainerDataType : IGenericContainerDataType
{
	public IInstancedContainerDataType Instantiate(IPrimitiveDataType keyType, IDataType valueType)
		=> new InstancedContainerDataType
		{
			Name = Name,
			GenericType = this,
			KeyType = keyType,
			ValueType = valueType,
		};
	
	public required string Name { get; init; }
	public string Namespace => string.Empty;
	public required EContainerType ContainerType { get; init; }
}


