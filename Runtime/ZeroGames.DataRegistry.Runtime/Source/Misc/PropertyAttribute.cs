// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Runtime;

[AttributeUsage(AttributeTargets.Property)]
public class PropertyAttribute : Attribute
{
	public string? Default { get; init; }
}


