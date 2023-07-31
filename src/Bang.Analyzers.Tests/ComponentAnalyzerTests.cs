using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bang.Analyzers.Tests;

using Verify = BangAnalyzerVerifier<ComponentAnalyzer>;

[TestClass]
public sealed class ComponentAnalyzerTests
{
	[TestMethod(displayName: "Readonly structs do not trigger the analyzer.")]
	public async Task ReadOnlyStructsDontTriggerTheAnalyzer()
	{
		const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

public readonly struct ReadonlyStructComponent : IComponent { }";
		await Verify.VerifyAnalyzerAsync(source);
	}
	
	[TestMethod(displayName: "Readonly record structs do not trigger the analyzer.")]
	public async Task ReadOnlyRecordStructsDontTriggerTheAnalyzer()
	{
		const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

public readonly record struct ReadonlyStructComponent : IComponent;";
		await Verify.VerifyAnalyzerAsync(source);
	}

	[TestMethod(displayName: "Classes cannot be components.")]
	public async Task ClassesCannotBeComponents()
	{
		const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

class ClassComponent: IComponent { }";

		var expected = Verify.Diagnostic(ComponentAnalyzer.ClassesCannotBeComponents)
			.WithSeverity(DiagnosticSeverity.Warning)
			.WithSpan(6, 7, 6, 21);

		await Verify.VerifyAnalyzerAsync(source, expected);
	}

	[TestMethod(displayName: "Nested classes cannot be components.")]
	public async Task NestedClassesCannotBeComponents()
	{
		const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

class NestedComponent: BaseClass { }
class BaseClass : IComponent { }";

		var expected = new[]
		{
			Verify.Diagnostic(ComponentAnalyzer.ClassesCannotBeComponents)
				.WithSeverity(DiagnosticSeverity.Warning)
				.WithSpan(6, 7, 6, 22),
			Verify.Diagnostic(ComponentAnalyzer.ClassesCannotBeComponents)
				.WithSeverity(DiagnosticSeverity.Warning)
				.WithSpan(7, 7, 7, 16)
		};

		await Verify.VerifyAnalyzerAsync(source, expected);
	}

	[TestMethod(displayName: "Indirect implementations of IComponent on classes still trigger the analyzer.")]
	public async Task IndirectImplementationOnClasses()
	{
		const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

class ClassComponent: INestedComponent { }
interface INestedComponent : IComponent { }";

		var expected = Verify
			.Diagnostic(ComponentAnalyzer.ClassesCannotBeComponents)
			.WithSeverity(DiagnosticSeverity.Warning)
			.WithSpan(6, 7, 6, 21);

		await Verify.VerifyAnalyzerAsync(source, expected);
	}

	[TestMethod(displayName: "Record classes cannot be components.")]
	public async Task RecordClassesCannotBeComponents()
	{
		const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

record class ClassComponent: IComponent;";

		var expected = Verify.Diagnostic(ComponentAnalyzer.ClassesCannotBeComponents)
			.WithSeverity(DiagnosticSeverity.Warning)
			.WithSpan(6, 14, 6, 28);

		await Verify.VerifyAnalyzerAsync(source, expected);
	}

	[TestMethod(displayName: "Nested record classes cannot be components.")]
	public async Task NestedRecordClassesCannotBeComponents()
	{
		const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

record class NestedComponent: BaseRecord { }
record class BaseRecord : IComponent { }";

		var expected = new[]
		{
			Verify.Diagnostic(ComponentAnalyzer.ClassesCannotBeComponents)
				.WithSeverity(DiagnosticSeverity.Warning)
				.WithSpan(6, 14, 6, 29),
			Verify.Diagnostic(ComponentAnalyzer.ClassesCannotBeComponents)
				.WithSeverity(DiagnosticSeverity.Warning)
				.WithSpan(7, 14, 7, 24)
		};

		await Verify.VerifyAnalyzerAsync(source, expected);
	}

	[TestMethod(displayName: "Indirect implementations of IComponent on record classes still trigger the analyzer.")]
	public async Task IndirectImplementationOnRecordClasses()
	{
		const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

record class RecordClassComponent: INestedComponent;
interface INestedComponent : IComponent { }";

		var expected = Verify
			.Diagnostic(ComponentAnalyzer.ClassesCannotBeComponents)
			.WithSeverity(DiagnosticSeverity.Warning)
			.WithSpan(6, 14, 6, 34);

		await Verify.VerifyAnalyzerAsync(source, expected);
	}

	[TestMethod(displayName: "Indirect implementations of IComponent on structs still trigger the analyzer.")]
	public async Task IndirectImplementationOnStructs()
	{
		const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

struct RecordClassComponent: INestedComponent { }
interface INestedComponent : IComponent { }";

		var expected = Verify
			.Diagnostic(ComponentAnalyzer.StructsMustBeReadonly)
			.WithSeverity(DiagnosticSeverity.Warning)
			.WithSpan(6, 8, 6, 28);

		await Verify.VerifyAnalyzerAsync(source, expected);
	}
	
	[TestMethod(displayName: "Indirect implementations of IComponent on record structs still trigger the analyzer.")]
	public async Task IndirectImplementationOnRecordStructs()
	{
		const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

record struct RecordClassComponent: INestedComponent;
interface INestedComponent : IComponent { }";

		var expected = Verify
			.Diagnostic(ComponentAnalyzer.StructsMustBeReadonly)
			.WithSeverity(DiagnosticSeverity.Warning)
			.WithSpan(6, 15, 6, 35);

		await Verify.VerifyAnalyzerAsync(source, expected);
	}

	[TestMethod(displayName: "Structs must be declared as readonly.")]
	public async Task StructsMustBeReadonly()
	{
		const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

struct Component: IComponent { }";

		var expected = Verify
			.Diagnostic(ComponentAnalyzer.StructsMustBeReadonly)
			.WithSeverity(DiagnosticSeverity.Warning)
			.WithSpan(6, 8, 6, 17);

		await Verify.VerifyAnalyzerAsync(source, expected);
	}

	[TestMethod(displayName: "Record Structs must be declared readonly.")]
	public async Task RecordStructsMustBeReadonly()
	{
		const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

record struct Component : Bang.Components.IComponent;";

		var expected = Verify
			.Diagnostic(ComponentAnalyzer.StructsMustBeReadonly)
			.WithSeverity(DiagnosticSeverity.Warning)
			.WithSpan(6, 15, 6, 24);

		await Verify.VerifyAnalyzerAsync(source, expected);
	}
}