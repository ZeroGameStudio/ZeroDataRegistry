// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler;

public interface ISchemaSourceResolver
{
	SchemaSourceResolveResult Resolve(SchemaSourceUri uri);
}


