// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Runtime;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
public class SchemaAttribute(Type schema) : Attribute
{
	public Type Schema { get; } = schema;
}


