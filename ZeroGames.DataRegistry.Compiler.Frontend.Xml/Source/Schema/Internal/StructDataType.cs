// Copyright Zero Games. All Rights Reserved.

using ZeroGames.DataRegistry.Compiler.Core;

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

internal sealed class StructDataType : CompositeDataTypeBase, IStructDataType
{
	public override bool SetupDependencies()
	{
		BaseType = BaseTypeFactory();
		return true;
	}

	public IStructDataType? BaseType { get; private set; }
	public required Func<IStructDataType?> BaseTypeFactory { private get; init; }
	
	protected override IStructDataType? InternalBaseType => BaseType;
}


