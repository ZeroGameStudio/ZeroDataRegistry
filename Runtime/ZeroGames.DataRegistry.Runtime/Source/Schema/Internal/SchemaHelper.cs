// Copyright Zero Games. All Rights Reserved.

using System.Reflection;

namespace ZeroGames.DataRegistry.Runtime;

internal static class SchemaHelper
{

	public static Type GetImplementationType(Type rootType, string implementationTypeName)
	{
		Type rootTypeSchema = rootType.GetCustomAttribute<SchemaAttribute>()!.Schema;
		if (implementationTypeName == rootType.Name)
		{
			return rootType;
		}
	
		Type implementationType = rootTypeSchema.GetCustomAttribute<DataTypesAttribute>()![implementationTypeName];
		return !implementationType.IsAbstract && implementationType.IsAssignableTo(rootType) ? implementationType : throw new InvalidOperationException();
	}
	
}


