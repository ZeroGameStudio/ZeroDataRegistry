// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Runtime;

public interface IRegistryElement : IDisposable
{
	IRegistry Registry { get; }
	string Name { get; }
	bool IsDisposed { get; }
}

public static class RegistryElementExtensions
{
	public static void GuardInvariant(this IRegistryElement @this)
	{
		if (@this.IsDisposed)
		{
			throw new ObjectDisposedException($"{@this.Name} is disposed.");
		}
	}
}


