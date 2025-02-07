// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

internal class InterfaceDataType : CompositeDataTypeBase, IInterfaceDataType
{
	protected override bool InternalSetupDependencies() => true;
	protected override IDataType? InternalBaseType => null;
}


