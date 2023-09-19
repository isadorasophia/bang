using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bang.Analyzers.CodeFixProviders;
using Bang.Analyzers.Analyzers;

namespace Bang.Analyzers.Tests.CodeFixProviders;

using Verify = BangCodeFixProviderVerifier<SystemAnalyzer, RemoveAttributeCodeFixProvider>;

[TestClass]
public sealed class RemoveAttributeCodeFixProviderTests
{
    [TestMethod(displayName: "Non-Messager systems annotated with the Messager attribute trigger a warning.")]
    public async Task AnnotatedNonMessagerSystems()
    {
        const string source = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public readonly record struct CorrectMessage : IMessage;

[Filter(ContextAccessorKind.Read, typeof(CorrectMessage))]
[Messager(typeof(CorrectMessage))]
public class IncorrectSystem : ISystem
{
}";
        const string codeFix = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public readonly record struct CorrectMessage : IMessage;

[Filter(ContextAccessorKind.Read, typeof(CorrectMessage))]
public class IncorrectSystem : ISystem
{
}";

        var expected = Verify.Diagnostic(SystemAnalyzer.NonApplicableMessagerAttribute)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithSpan(13, 2, 13, 34);

        await Verify.VerifyCodeFixAsync(source, expected, codeFix);
    }

    [TestMethod(displayName: "Non-Reactive systems annotated with the Watch attribute trigger a warning.")]
    public async Task AnnotatedNonReactiveSystems()
    {
        const string source = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public readonly record struct CorrectComponent : IComponent;

[Watch(typeof(CorrectComponent))]
[Filter(ContextAccessorKind.Read, typeof(CorrectComponent))]
public class IncorrectSystem : ISystem
{
}";
        const string codeFix = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public readonly record struct CorrectComponent : IComponent;

[Filter(ContextAccessorKind.Read, typeof(CorrectComponent))]
public class IncorrectSystem : ISystem
{
}";

        var expected = Verify.Diagnostic(SystemAnalyzer.NonApplicableWatchAttribute)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithSpan(12, 2, 12, 33);

        await Verify.VerifyCodeFixAsync(source, expected, codeFix);
    }
}