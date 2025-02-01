// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Core;

public interface ICompilationContext
{
	IPrimitiveDataType GetPrimitiveDataType(string name);
	ISchema GetSchema(SchemaSourceUri uri);
	ISchema GetSchema(string name);
	
	VoidDataType VoidDataType { get; }
	
	IGenericContainerDataType GenericListType { get; }
	IGenericContainerDataType GenericSetType { get; }
	IGenericContainerDataType GenericMapType { get; }
	IGenericContainerDataType GenericOptionalType { get; }
}


