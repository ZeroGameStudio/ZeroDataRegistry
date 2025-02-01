// Copyright Zero Games. All Rights Reserved.

using ZeroGames.DataRegistry.Compiler.Core;

namespace ZeroGames.DataRegistry.Compiler.Server;

internal class CompilationContext : ICompilationContext
{

	public IPrimitiveDataType GetPrimitiveDataType(string name)
		=> _primitiveMap[name];

	public ISchema GetSchema(SchemaSourceUri uri)
		=> _schemaMap[uri];

	public ISchema GetSchema(string name)
		=> _schemaLookup[name];

	public void RegisterPrimitiveDataType(IPrimitiveDataType type)
	{
		_primitiveMap[type.Name] = type;
	}

	public void RegisterSchema(SchemaSourceUri uri, ISchema schema)
	{
		_schemaMap[uri] = schema;
		_schemaLookup[schema.Name] = schema;
	}

	public VoidDataType VoidDataType { get; } = new();
	
	public required IGenericContainerDataType GenericListType { get; init; }
	public required IGenericContainerDataType GenericSetType { get; init; }
	public required IGenericContainerDataType GenericMapType { get; init; }
	public required IGenericContainerDataType GenericOptionalType { get; init; }

	private readonly Dictionary<string, IPrimitiveDataType> _primitiveMap = new();
	
	private readonly Dictionary<SchemaSourceUri, ISchema> _schemaMap = new();
	private readonly Dictionary<string, ISchema> _schemaLookup = new();

}


