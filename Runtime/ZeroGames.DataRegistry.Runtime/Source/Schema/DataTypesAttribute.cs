// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Runtime;

[AttributeUsage(AttributeTargets.Class)]
public class DataTypesAttribute(params Type[] dataTypes) : Attribute
{
	public Type this[string typeName] => _dataTypeByTypeName[typeName];
	private readonly Dictionary<string, Type> _dataTypeByTypeName = dataTypes.ToDictionary(t => t.Name, t => t);
}


