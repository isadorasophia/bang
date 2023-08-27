using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bang.Analyzers.Tests;

using Verify = BangAnalyzerVerifier<MessageAnalyzer>;

[TestClass]
public sealed class MessageAnalyzerTests
{
    [TestMethod(displayName: "Readonly structs do not trigger the analyzer.")]
    public async Task ReadOnlyStructsDontTriggerTheAnalyzer()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

public readonly struct ReadonlyStructMessage : IMessage { }";
        await Verify.VerifyAnalyzerAsync(source);
    }

    [TestMethod(displayName: "Readonly record structs do not trigger the analyzer.")]
    public async Task ReadOnlyRecordStructsDontTriggerTheAnalyzer()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

public readonly record struct ReadonlyStructMessage : IMessage;";
        await Verify.VerifyAnalyzerAsync(source);
    }

    [TestMethod(displayName: "Classes cannot be messages.")]
    public async Task ClassesCannotBeMessages()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

class ClassMessage: IMessage { }";

        var expected = Verify.Diagnostic(MessageAnalyzer.ClassesCannotBeMessages)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(6, 7, 6, 19);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Nested classes cannot be messages.")]
    public async Task NestedClassesCannotBeMessages()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

class NestedMessage: BaseClass { }
class BaseClass : IMessage { }";

        var expected = new[]
        {
            Verify.Diagnostic(MessageAnalyzer.ClassesCannotBeMessages)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithSpan(6, 7, 6, 20),
            Verify.Diagnostic(MessageAnalyzer.ClassesCannotBeMessages)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithSpan(7, 7, 7, 16)
        };

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Indirect implementations of IMessage on classes still trigger the analyzer.")]
    public async Task IndirectImplementationOnClasses()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

class ClassMessage: INestedMessage { }
interface INestedMessage : IMessage { }";

        var expected = Verify
            .Diagnostic(MessageAnalyzer.ClassesCannotBeMessages)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(6, 7, 6, 19);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Record classes cannot be messages.")]
    public async Task RecordClassesCannotBeMessages()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

record class ClassMessage: IMessage;";

        var expected = Verify.Diagnostic(MessageAnalyzer.ClassesCannotBeMessages)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(6, 14, 6, 26);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Nested record classes cannot be messages.")]
    public async Task NestedRecordClassesCannotBeMessages()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

record class NestedMessage: BaseRecord { }
record class BaseRecord : IMessage { }";

        var expected = new[]
        {
            Verify.Diagnostic(MessageAnalyzer.ClassesCannotBeMessages)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithSpan(6, 14, 6, 27),
            Verify.Diagnostic(MessageAnalyzer.ClassesCannotBeMessages)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithSpan(7, 14, 7, 24)
        };

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Indirect implementations of IMessage on record classes still trigger the analyzer.")]
    public async Task IndirectImplementationOnRecordClasses()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

record class RecordClassMessage: INestedMessage;
interface INestedMessage : IMessage { }";

        var expected = Verify
            .Diagnostic(MessageAnalyzer.ClassesCannotBeMessages)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(6, 14, 6, 32);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Indirect implementations of IMessage on structs still trigger the analyzer.")]
    public async Task IndirectImplementationOnStructs()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

struct RecordClassMessage: INestedMessage { }
interface INestedMessage : IMessage { }";

        var expected = Verify
            .Diagnostic(MessageAnalyzer.MessagesMustBeReadonly)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(6, 8, 6, 26);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Indirect implementations of IMessage on record structs still trigger the analyzer.")]
    public async Task IndirectImplementationOnRecordStructs()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

record struct RecordClassMessage: INestedMessage;
interface INestedMessage : IMessage { }";

        var expected = Verify
            .Diagnostic(MessageAnalyzer.MessagesMustBeReadonly)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(6, 15, 6, 33);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Structs must be declared as readonly.")]
    public async Task StructsMustBeReadonly()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

struct Message : IMessage { }";

        var expected = Verify
            .Diagnostic(MessageAnalyzer.MessagesMustBeReadonly)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(6, 8, 6, 15);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Record Structs must be declared readonly.")]
    public async Task RecordStructsMustBeReadonly()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

record struct Message : IMessage;";

        var expected = Verify
            .Diagnostic(MessageAnalyzer.MessagesMustBeReadonly)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(6, 15, 6, 22);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }
}