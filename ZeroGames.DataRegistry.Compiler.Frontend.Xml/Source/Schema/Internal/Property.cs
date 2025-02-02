// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

internal sealed class Property : SchemaElementBase, IProperty, ISetupDependenciesSource
{
	public bool SetupDependencies()
	{
		Type = TypeFactory();
		return Role != EPropertyRole.PrimaryKey || Type is IPrimitiveDataType { CanBeKey: true };
	}
	
	public required EPropertyRole Role { get; init; }
	public IDataType Type { get; private set; } = null!;
	public required Func<IDataType> TypeFactory { private get; init; }
	public required string DefaultValue { get; init; }
}


