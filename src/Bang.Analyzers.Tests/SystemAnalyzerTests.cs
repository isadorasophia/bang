using Microsoft.CodeAnalysis;
using Xunit;

namespace Bang.Analyzers.Tests;

using Verify = BangAnalyzerVerifier<SystemAnalyzer>;

public sealed class SystemAnalyzerTests
{
	public static readonly IEnumerable<object[]> ValidSystemImplementations = new [] 
	{
		// IReactiveSystem with proper annotation
		new object[] 
		{ 
			@"
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
}"
		},
		// IMessagerSystem with proper annotation
		new object[] {
			@"
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
}"
		}
	};

	[Theory(DisplayName = "Properly annotated systems do not trigger the analyzer.")]
	[MemberData(nameof(ValidSystemImplementations))]
	public async Task ProperlyAnnotatedSystemsDoNotTriggerTheAnalyzer(string source)
	{
		await Verify.VerifyAnalyzerAsync(source);
	}

	[Fact(DisplayName = "Messager systems must be annotated with the Messager attribute.")]
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
			.WithSeverity(DiagnosticSeverity.Warning)
			.WithSpan(9, 1, 12, 2);
		
		await Verify.VerifyAnalyzerAsync(source, expected);
	}

	[Fact(DisplayName = "Reactive systems must be annotated with the Watch attribute.")]
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
			.WithSeverity(DiagnosticSeverity.Warning)
			.WithSpan(10, 1, 15, 2);
		
		await Verify.VerifyAnalyzerAsync(source, expected);
	}
}