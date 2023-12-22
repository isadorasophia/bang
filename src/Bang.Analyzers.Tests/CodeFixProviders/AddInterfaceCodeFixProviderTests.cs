using Bang.Analyzers.Analyzers;
using Bang.Analyzers.CodeFixProviders;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bang.Analyzers.Tests.CodeFixProviders;

using Verify = BangCodeFixProviderVerifier<AttributeAnalyzer, AddInterfaceCodeFixProvider>;

[TestClass]
public sealed class AddInterfaceCodeFixProviderTests
{
    [TestMethod(displayName: "Non-Component types annotated with the Unique attribute trigger a warning.")]
    public async Task AnnotatedNonComponents()
    {
        const string source = @"
using Bang;
using Bang.Components;

namespace BangAnalyzerTestNamespace;

[Unique]
public readonly struct IncorrectUnique { }";
        const string codeFix = @"
using Bang;
using Bang.Components;

namespace BangAnalyzerTestNamespace;

[Unique]
public readonly struct IncorrectUnique : IComponent { }";

        var expected = Verify.Diagnostic(AttributeAnalyzer.UniqueAttributeOnNonComponent)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(7, 2, 7, 8);

        await Verify.VerifyCodeFixAsync(source, expected, codeFix);
    }

    [TestMethod(displayName: "The code fix works even if there already is a base type.")]
    public async Task MultipleBaseTypes()
    {
        const string source = @"
using Bang;
using Bang.Components;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

[Unique]
public readonly struct IncorrectUnique : ISystem { }";
        const string codeFix = @"
using Bang;
using Bang.Components;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

[Unique]
public readonly struct IncorrectUnique : ISystem, IComponent { }";

        var expected = Verify.Diagnostic(AttributeAnalyzer.UniqueAttributeOnNonComponent)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(8, 2, 8, 8);

        await Verify.VerifyCodeFixAsync(source, expected, codeFix);
    }

    [TestMethod(displayName: "The code fix works even when there are multiple annotations.")]
    public async Task MultipleBaseTypesError()
    {
        const string source = @"
using System;
using Bang;
using Bang.Components;

namespace BangAnalyzerTestNamespace;

[Obsolete(""Blah""), Unique]
public readonly struct IncorrectUnique { }";
        const string codeFix = @"
using System;
using Bang;
using Bang.Components;

namespace BangAnalyzerTestNamespace;

[Obsolete(""Blah""), Unique]
public readonly struct IncorrectUnique : IComponent { }";

        var expected = Verify.Diagnostic(AttributeAnalyzer.UniqueAttributeOnNonComponent)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(8, 20, 8, 26);

        await Verify.VerifyCodeFixAsync(source, expected, codeFix);
    }
}