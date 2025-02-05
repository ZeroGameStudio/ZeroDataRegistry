// Copyright Zero Games. All Rights Reserved.

using System.Collections.Immutable;
using ZeroGames.DataRegistry.Compiler.Backend;

namespace ZeroGames.DataRegistry.Compiler.Server;

internal class Compiler : ICompiler
{

	public Compiler(in CompilerOptions options)
	{
		_options = options;
		_context = new()
		{
			GenericListType = options.GenericListType,
			GenericSetType = options.GenericSetType,
			GenericMapType = options.GenericMapType,
			GenericOptionalType = options.GenericOptionalType,
		};
		
		foreach (var (_, frontend) in options.Frontend)
		{
			frontend.CompilationContext = _context;
		}
		
		options.Backend.CompilationContext = _context;

		foreach (var primitive in options.Primitives)
		{
			_context.RegisterPrimitiveDataType(primitive);
		}
	}
	
	public async IAsyncEnumerable<CompilationUnitResult> CompileAsync(IReadOnlySet<SchemaSourceUri> sources)
	{
		await Parse(sources);
		await foreach (var result in Emit(sources))
		{
			yield return result;
		}
	}

	private record CompilationUnit(SchemaSourceUri Uri, Stream Source, SchemaSourceForm Form)
	{
		public IEnumerable<CompilationUnit> Dependencies { get; set; } = [];
	}
	
	private static CompilationUnit[] TopologicalSort(IReadOnlySet<CompilationUnit> compilations)
	{
		var inDegreeMap = new Dictionary<CompilationUnit, int32>();
		foreach (var compilation in compilations)
		{
			inDegreeMap.TryAdd(compilation, 0);
			foreach (var dependency in compilation.Dependencies)
			{
				inDegreeMap.TryAdd(dependency, 0);
				inDegreeMap[dependency]++;
			}
		}

		Queue<CompilationUnit> queue = new();
		foreach (var (compilation, degree) in inDegreeMap)
		{
			if (degree == 0)
			{
				queue.Enqueue(compilation);
			}
		}

		var result = new CompilationUnit[compilations.Count];
		int32 iResult = compilations.Count;
		while (queue.TryDequeue(out var current))
		{
			result[--iResult] = current;
			foreach (var dependency in current.Dependencies)
			{
				if (--inDegreeMap[dependency] == 0)
				{
					queue.Enqueue(dependency);
				}
			}
		}

		if (iResult != 0)
		{
			throw new ArgumentException("The graph contains a cycle.");
		}

		return result;
	}

	private async Task Parse(IReadOnlySet<SchemaSourceUri> sources)
	{
		Dictionary<SchemaSourceUri, CompilationUnit> compilationMap = new();

		async Task<CompilationUnit?> LoadCompilationUnit(SchemaSourceUri uri)
		{
			if (compilationMap.ContainsKey(uri))
			{
				return null;
			}
			
			SchemaSourceResolveResult result = _options.SchemaSourceResolver.Resolve(uri);
			CompilationUnit unit = new(uri, result.Source, result.Form);
			compilationMap[uri] = unit;
			
			List<CompilationUnit>? dependencies = null;
			foreach (var usingUri in await _options.Frontend[result.Form].GetImportsAsync(result.Source))
			{
				if (await LoadCompilationUnit(usingUri) is { } dependency)
				{
					dependencies ??= new();
					dependencies.Add(dependency);
				}
			}

			if (dependencies is not null)
			{
				unit.Dependencies = dependencies;
			}

			return unit;
		}
		
		foreach (var sourceUri in sources)
		{
			await LoadCompilationUnit(sourceUri);
		}

		CompilationUnit[] sortedCompilationUnits;
		try
		{
			sortedCompilationUnits = TopologicalSort(compilationMap.Values.ToImmutableHashSet());
		}
		catch (Exception ex)
		{
			throw new CompilationException("Schema source has circular dependency.", ex);
		}

		foreach (var compilationUnit in sortedCompilationUnits)
		{
			ISchema schema = await _options.Frontend[compilationUnit.Form].CompileAsync(compilationUnit.Uri, compilationUnit.Source);
			_context.RegisterSchema(compilationUnit.Uri, schema);
		}
	}

	private async IAsyncEnumerable<CompilationUnitResult> Emit(IReadOnlySet<SchemaSourceUri> sources)
	{
		// @FIXME: Parallel.
		foreach (var sourceUri in sources)
		{
			ISchema schema = _context.GetSchema(sourceUri);
			await foreach (var result in _options.Backend.CompileAsync(schema))
			{
				yield return result;
			}
		}
	}

	// private ICompilerFrontend GetFrontend(SchemaSourceForm form)
	// {
	// 	if (!_frontendMap.TryGetValue(form, out var frontend))
	// 	{
	// 		try
	// 		{
	// 			Type frontendType = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.Assemblies
	// 				.SelectMany(asm => asm.GetTypes())
	// 				.Single(type => type.IsAssignableTo(typeof(ICompilerFrontend)) &&
	// 				                type.GetCustomAttribute<CompilerFrontendAttribute>() is { } attr &&
	// 				                attr.SupportedForm == form);
	// 			frontend = (ICompilerFrontend)Activator.CreateInstance(frontendType)!;
	// 		}
	// 		catch (Exception ex)
	// 		{
	// 			throw new CompilationException($"Compiler frontend for form {form.Name} not found.", ex);
	// 		}
	//
	// 		_frontendMap[form] = frontend;
	// 	}
	//
	// 	return frontend;
	
	// }

	private readonly CompilerOptions _options;

	private readonly CompilationContext _context;

}


