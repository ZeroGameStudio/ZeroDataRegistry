// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.DataRegistry.Compiler.Backend;

public readonly record struct CompilationUnitResult(Stream Dest, ECompilationErrorLevel ErrorLevel, string Message, IReadOnlyDictionary<string, string> Properties);


