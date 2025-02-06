// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Runtime;

public static class TypeExtensions
{

	public static bool IsAssignableToSomeGenericInstanceOf(this Type @this, Type targetType)
	{
		if (!targetType.IsGenericTypeDefinition)
		{
			throw new ArgumentOutOfRangeException(nameof(targetType));
		}

		if (targetType == typeof(Nullable<>))
		{
			return @this.IsValueType;
		}
		
		// Check whether any base type of this type is an instance of targetType.
		Type? currentType = @this;
		while (currentType is not null)
		{
			if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == targetType)
			{
				return true;
			}
				
			currentType = currentType.BaseType;
		}

		// Check whether any implemented interface of this type is an instance of targetType.
		return @this.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == targetType);
	}

	public static Type? GetGenericInstanceOf(this Type @this, Type targetType)
	{
		if (!targetType.IsGenericTypeDefinition)
		{
			throw new ArgumentOutOfRangeException(nameof(targetType));
		}

		// Check whether any base type of this type is an instance of targetType.
		Type? currentType = @this;
		while (currentType is not null)
		{
			if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == targetType)
			{
				return currentType;
			}
				
			currentType = currentType.BaseType;
		}

		// Check whether any implemented interface of this type is an instance of targetType.
		return @this.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == targetType);
	}
	
}


