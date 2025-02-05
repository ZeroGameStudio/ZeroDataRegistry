// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler;

public interface IProperty : ISchemaElement
{
	EPropertyRole Role { get; }
	IDataType Type { get; }
	string DefaultValue { get; }
}


