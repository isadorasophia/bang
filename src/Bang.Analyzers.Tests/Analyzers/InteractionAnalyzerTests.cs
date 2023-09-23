using Bang.Analyzers.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bang.Analyzers.Tests.Analyzers;

using Verify = BangAnalyzerVerifier<InteractionAnalyzer>;

[TestClass]
public sealed class InteractionAnalyzerTests
{
    [TestMethod(displayName: "Readonly structs do not trigger the analyzer.")]
    public async Task ReadOnlyStructsDontTriggerTheAnalyzer()
    {
        const string source = @"
using Bang;
using Bang.Entities;
using Bang.Interactions;

namespace BangAnalyzerTestNamespace;

public readonly struct ReadonlyStructInteraction : IInteraction
{
    public void Interact(World world, Entity interactor, Entity? interacted) { }
}";
        await Verify.VerifyAnalyzerAsync(source);
    }

    [TestMethod(displayName: "Readonly record structs do not trigger the analyzer.")]
    public async Task ReadOnlyRecordStructsDontTriggerTheAnalyzer()
    {
        const string source = @"
using Bang;
using Bang.Entities;
using Bang.Interactions;

namespace BangAnalyzerTestNamespace;

public readonly record struct ReadonlyStructInteraction : IInteraction
{
    public void Interact(World world, Entity interactor, Entity? interacted) { }
}";
        await Verify.VerifyAnalyzerAsync(source);
    }

    [TestMethod(displayName: "Classes cannot be messages.")]
    public async Task ClassesCannotBeInteractions()
    {
        const string source = @"
using Bang;
using Bang.Entities;
using Bang.Interactions;

namespace BangAnalyzerTestNamespace;

class ClassInteraction: IInteraction
{
    public void Interact(World world, Entity interactor, Entity? interacted) { }
}";

        var expected = Verify.Diagnostic(InteractionAnalyzer.ClassesCannotBeInteractions)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(8, 7, 8, 23);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Nested classes cannot be messages.")]
    public async Task NestedClassesCannotBeInteractions()
    {
        const string source = @"
using Bang;
using Bang.Entities;
using Bang.Interactions;

namespace BangAnalyzerTestNamespace;

class NestedInteraction: BaseClass { }
class BaseClass : IInteraction
{
    public void Interact(World world, Entity interactor, Entity? interacted) { }
}";

        var expected = new[]
        {
            Verify.Diagnostic(InteractionAnalyzer.ClassesCannotBeInteractions)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithSpan(8, 7, 8, 24),
            Verify.Diagnostic(InteractionAnalyzer.ClassesCannotBeInteractions)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithSpan(9, 7, 9, 16)
        };

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Indirect implementations of IInteraction on classes still trigger the analyzer.")]
    public async Task IndirectImplementationOnClasses()
    {
        const string source = @"
using Bang;
using Bang.Entities;
using Bang.Interactions;

namespace BangAnalyzerTestNamespace;

class ClassInteraction: INestedInteraction
{
    public void Interact(World world, Entity interactor, Entity? interacted) { }
}
interface INestedInteraction : IInteraction { }";

        var expected = Verify
            .Diagnostic(InteractionAnalyzer.ClassesCannotBeInteractions)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(8, 7, 8, 23);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Record classes cannot be messages.")]
    public async Task RecordClassesCannotBeInteractions()
    {
        const string source = @"
using Bang;
using Bang.Entities;
using Bang.Interactions;

namespace BangAnalyzerTestNamespace;

record class ClassInteraction : IInteraction
{
    public void Interact(World world, Entity interactor, Entity? interacted) { }
}";

        var expected = Verify.Diagnostic(InteractionAnalyzer.ClassesCannotBeInteractions)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(8, 14, 8, 30);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Nested record classes cannot be messages.")]
    public async Task NestedRecordClassesCannotBeInteractions()
    {
        const string source = @"
using Bang;
using Bang.Entities;
using Bang.Interactions;

namespace BangAnalyzerTestNamespace;

record class NestedInteraction: BaseRecord { }
record class BaseRecord : IInteraction
{
    public void Interact(World world, Entity interactor, Entity? interacted) { }
}";

        var expected = new[]
        {
            Verify.Diagnostic(InteractionAnalyzer.ClassesCannotBeInteractions)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithSpan(8, 14, 8, 31),
            Verify.Diagnostic(InteractionAnalyzer.ClassesCannotBeInteractions)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithSpan(9, 14, 9, 24)
        };

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Indirect implementations of IInteraction on record classes still trigger the analyzer.")]
    public async Task IndirectImplementationOnRecordClasses()
    {
        const string source = @"
using Bang;
using Bang.Entities;
using Bang.Interactions;

namespace BangAnalyzerTestNamespace;

record class RecordClassInteraction: INestedInteraction
{
    public void Interact(World world, Entity interactor, Entity? interacted) { }
}
interface INestedInteraction : IInteraction { }";

        var expected = Verify
            .Diagnostic(InteractionAnalyzer.ClassesCannotBeInteractions)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(8, 14, 8, 36);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Indirect implementations of IInteraction on structs still trigger the analyzer.")]
    public async Task IndirectImplementationOnStructs()
    {
        const string source = @"
using Bang;
using Bang.Entities;
using Bang.Interactions;

namespace BangAnalyzerTestNamespace;

struct RecordClassInteraction: INestedInteraction
{
    public void Interact(World world, Entity interactor, Entity? interacted) { }
}
interface INestedInteraction : IInteraction { }";

        var expected = Verify
            .Diagnostic(InteractionAnalyzer.InteractionsMustBeReadonly)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(8, 8, 8, 30);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Indirect implementations of IInteraction on record structs still trigger the analyzer.")]
    public async Task IndirectImplementationOnRecordStructs()
    {
        const string source = @"
using Bang;
using Bang.Entities;
using Bang.Interactions;

namespace BangAnalyzerTestNamespace;

record struct RecordClassInteraction: INestedInteraction
{
    public void Interact(World world, Entity interactor, Entity? interacted) { }
}
interface INestedInteraction : IInteraction { }";

        var expected = Verify
            .Diagnostic(InteractionAnalyzer.InteractionsMustBeReadonly)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(8, 15, 8, 37);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Structs must be declared as readonly.")]
    public async Task StructsMustBeReadonly()
    {
        const string source = @"
using Bang;
using Bang.Entities;
using Bang.Interactions;

namespace BangAnalyzerTestNamespace;

struct Interaction : IInteraction
{
    public void Interact(World world, Entity interactor, Entity? interacted) { }
}";

        var expected = Verify
            .Diagnostic(InteractionAnalyzer.InteractionsMustBeReadonly)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(8, 8, 8, 19);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Record Structs must be declared readonly.")]
    public async Task RecordStructsMustBeReadonly()
    {
        const string source = @"
using Bang;
using Bang.Entities;
using Bang.Interactions;

namespace BangAnalyzerTestNamespace;

record struct Interaction : IInteraction
{
    public void Interact(World world, Entity interactor, Entity? interacted) { }
}";

        var expected = Verify
            .Diagnostic(InteractionAnalyzer.InteractionsMustBeReadonly)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(8, 15, 8, 26);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }
}