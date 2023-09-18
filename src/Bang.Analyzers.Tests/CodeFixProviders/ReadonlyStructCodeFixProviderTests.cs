using Bang.Analyzers.Analyzers;
using Bang.Analyzers.CodeFixProviders;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bang.Analyzers.Tests.CodeFixProviders;

using Verify = BangCodeFixProviderVerifier<ComponentAnalyzer, ReadonlyStructCodeFixProvider>;

[TestClass]
public sealed class ReadonlyStructCodeFixProviderTests
{
    [TestMethod(displayName: "Indirect implementations of IComponent on structs still trigger the analyzer.")]
    public async Task IndirectImplementationOnStructs()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

struct RecordClassComponent: INestedComponent { }
interface INestedComponent : IComponent { }";

        const string codeFix = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

readonly struct RecordClassComponent: INestedComponent { }
interface INestedComponent : IComponent { }";

        var expected = Verify
            .Diagnostic(ComponentAnalyzer.ComponentsMustBeReadonly)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(6, 8, 6, 28);

        await Verify.VerifyCodeFixAsync(source, expected, codeFix);
    }

    [TestMethod(displayName: "Indirect implementations of IComponent on record structs still trigger the analyzer.")]
    public async Task IndirectImplementationOnRecordStructs()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

record struct RecordClassComponent: INestedComponent;
interface INestedComponent : IComponent { }";

        const string codeFix = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

readonly record struct RecordClassComponent: INestedComponent;
interface INestedComponent : IComponent { }";

        var expected = Verify
            .Diagnostic(ComponentAnalyzer.ComponentsMustBeReadonly)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(6, 15, 6, 35);

        await Verify.VerifyCodeFixAsync(source, expected, codeFix);
    }

    [TestMethod(displayName: "Structs must be declared as readonly.")]
    public async Task StructsMustBeReadonly()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

struct Component : IComponent { }";

        const string codeFix = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

readonly struct Component : IComponent { }";

        var expected = Verify
            .Diagnostic(ComponentAnalyzer.ComponentsMustBeReadonly)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(6, 8, 6, 17);

        await Verify.VerifyCodeFixAsync(source, expected, codeFix);
    }

    [TestMethod(displayName: "Record Structs must be declared readonly.")]
    public async Task RecordStructsMustBeReadonly()
    {
        const string source = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

public record struct Component : IComponent;";

        const string codeFix = @"
using Bang.Components;

namespace BangAnalyzerTestNamespace;

public readonly record struct Component : IComponent;";

        var expected = Verify
            .Diagnostic(ComponentAnalyzer.ComponentsMustBeReadonly)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(6, 22, 6, 31);

        await Verify.VerifyCodeFixAsync(source, expected, codeFix);
    }
}