// Copyright Zero Games. All Rights Reserved.

using ZeroGames.DataRegistry.Compiler.Core;

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

internal sealed class EntityDataType : CompositeDataTypeBase, IEntityDataType
{
	public override bool SetupDependencies()
	{
		BaseType = BaseTypeFactory();
		return true;
	}
	
	public IEntityDataType? BaseType { get; private set; }
	public required Func<IEntityDataType?> BaseTypeFactory { private get; init; }
	public required IReadOnlyList<IProperty> PrimaryKey { get; init; }

	protected override IEntityDataType? InternalBaseType => BaseType;
}


