// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

internal sealed class StructDataType : CompositeDataTypeBase, IStructDataType
{
	public IStructDataType? BaseType { get; private set; }
	public required Func<IStructDataType?> BaseTypeFactory { private get; init; }
	
	protected override bool InternalSetupDependencies()
	{
		BaseType = BaseTypeFactory();
		return true;
	}
	
	protected override IStructDataType? InternalBaseType => BaseType;
}


