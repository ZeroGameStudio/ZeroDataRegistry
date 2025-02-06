// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Server;

internal class CompilationContext : ICompilationContext
{

	public IPrimitiveDataType GetPrimitiveDataType(string name)
		=> _primitiveMap[name];

	public ISchema GetSchema(SchemaSourceUri uri)
		=> _schemaMap[uri];

	public void RegisterPrimitiveDataType(IPrimitiveDataType type)
		=> _primitiveMap[type.Name] = type;

	public void RegisterSchema(SchemaSourceUri uri, ISchema schema)
		=> _schemaMap[uri] = schema;

	public VoidDataType VoidDataType { get; } = new();
	
	public required IGenericContainerDataType GenericListType { get; init; }
	public required IGenericContainerDataType GenericSetType { get; init; }
	public required IGenericContainerDataType GenericMapType { get; init; }
	public required IGenericContainerDataType GenericOptionalType { get; init; }

	private readonly Dictionary<string, IPrimitiveDataType> _primitiveMap = new();
	private readonly Dictionary<SchemaSourceUri, ISchema> _schemaMap = new();

}


