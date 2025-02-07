// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

internal abstract class CompositeDataTypeBase : UserDefinedDataTypeBase, ICompositeDataType, ISetupDependenciesSource
{
	public bool SetupDependencies()
	{
		Interfaces = InterfaceFactory();
		return InternalSetupDependencies();
	}
	
	ICompositeDataType? ICompositeDataType.BaseType => InternalBaseType as ICompositeDataType;
	public required bool IsAbstract { get; init; }
	public IReadOnlyList<IInterfaceDataType> Interfaces { get; private set; } = null!;
	public required Func<IReadOnlyList<IInterfaceDataType>> InterfaceFactory { private get; init; }
	public required IReadOnlyList<IProperty> Properties { get; init; }

	protected abstract bool InternalSetupDependencies();
}


