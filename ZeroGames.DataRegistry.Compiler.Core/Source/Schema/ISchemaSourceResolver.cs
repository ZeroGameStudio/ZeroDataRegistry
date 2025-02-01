// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Core;

public interface ISchemaSourceResolver
{
	SchemaSourceResolveResult Resolve(SchemaSourceUri uri);
}


