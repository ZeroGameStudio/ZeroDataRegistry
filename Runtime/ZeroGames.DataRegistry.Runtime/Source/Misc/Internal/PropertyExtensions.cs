// Copyright Zero Games. All Rights Reserved.

using System.Reflection;

namespace ZeroGames.DataRegistry.Runtime;

public static class PropertyExtensions
{

	public static bool IsNullable(this PropertyInfo @this)
		=> new NullabilityInfoContext().Create(@this).ReadState != NullabilityState.NotNull;
	
}


