// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

internal abstract class CompositeDataTypeBase : UserDefinedDataTypeBase, ICompositeDataType, ISetupDependenciesSource
{
	public abstract bool SetupDependencies();
	
	public required bool IsAbstract { get; init; }
	public required IReadOnlyList<IProperty> Properties { get; init; }
}


