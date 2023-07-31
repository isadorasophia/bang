using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bang.Analyzers.Tests;

using Verify = BangAnalyzerVerifier<SystemAnalyzer>;

[TestClass]
public sealed class SystemAnalyzerTests
{
	private const string IReactiveSystemWithProperAnnotationSourceCode = @"
using Bang;
using Bang.Components;
using Bang.Entities;
using Bang.Systems;
using System.Collections.Immutable;

namespace BangAnalyzerTestNamespace;

public readonly record struct CorrectComponent : IComponent;

[Watch(typeof(CorrectComponent))]
public class CorrectReactiveSystem : IReactiveSystem
{
	public void OnAdded(World world, ImmutableArray<Entity> entities) { }
	public void OnRemoved(World world, ImmutableArray<Entity> entities) { }
	public void OnModified(World world, ImmutableArray<Entity> entities) { }
}";
	private const string IMessagerSystemWithProperAnnotationSourceCode = @"
using Bang;
using Bang.Components;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;
public readonly record struct CorrectMessage : IMessage;

[Messager(typeof(CorrectMessage))]
public class CorrectMessagerSystem : IMessagerSystem
{
	public void OnMessage(World world, Entity entity, IMessage message) { }
}";

	[TestMethod(displayName: "Properly annotated systems do not trigger the analyzer.")]
	[DataRow(IReactiveSystemWithProperAnnotationSourceCode)]
	[DataRow(IMessagerSystemWithProperAnnotationSourceCode)]
	public async Task ProperlyAnnotatedSystemsDoNotTriggerTheAnalyzer(string source)
	{
		await Verify.VerifyAnalyzerAsync(source);
	}

	[TestMethod(displayName: "Messager systems must be annotated with the Messager attribute.")]
	public async Task NonAnnotatedMessagerSystems()
	{
		const string source = @"
using Bang;
using Bang.Components;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public class IncorrectMessagerSystem : IMessagerSystem
{
	public void OnMessage(World world, Entity entity, IMessage message) { }
}";

		var expected = Verify.Diagnostic(SystemAnalyzer.MessagerAttribute)
			.WithSeverity(DiagnosticSeverity.Error)
			.WithSpan(9, 1, 12, 2);
		
		await Verify.VerifyAnalyzerAsync(source, expected);
	}

	[TestMethod(displayName: "Reactive systems must be annotated with the Watch attribute.")]
	public async Task NonAnnotatedReactiveSystems()
	{
		const string source = @"
using Bang;
using Bang.Components;
using Bang.Entities;
using Bang.Systems;
using System.Collections.Immutable;

namespace BangAnalyzerTestNamespace;

public class IncorrectReactiveSystem : IReactiveSystem
{
	public void OnAdded(World world, ImmutableArray<Entity> entities) { }
	public void OnRemoved(World world, ImmutableArray<Entity> entities) { }
	public void OnModified(World world, ImmutableArray<Entity> entities) { }
}";

		var expected = Verify.Diagnostic(SystemAnalyzer.WatchAttribute)
			.WithSeverity(DiagnosticSeverity.Error)
			.WithSpan(10, 1, 15, 2);
		
		await Verify.VerifyAnalyzerAsync(source, expected);
	}
}