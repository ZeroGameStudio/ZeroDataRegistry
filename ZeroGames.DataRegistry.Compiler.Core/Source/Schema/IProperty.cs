// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Core;

public interface IProperty : ISchemaElement
{
	EPropertyRole Role { get; }
	IDataType Type { get; }
	string DefaultValue { get; }
}


