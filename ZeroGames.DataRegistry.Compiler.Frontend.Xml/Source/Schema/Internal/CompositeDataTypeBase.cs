// Copyright Zero Games. All Rights Reserved.

using ZeroGames.DataRegistry.Compiler.Core;

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

internal abstract class CompositeDataTypeBase : UserDefinedDataTypeBase, ICompositeDataType, ISetupDependenciesSource
{
	public abstract bool SetupDependencies();
	
	public required IReadOnlyList<IProperty> Properties { get; init; }
}


