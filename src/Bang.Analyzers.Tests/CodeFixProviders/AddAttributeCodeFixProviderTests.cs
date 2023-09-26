
using Bang.Analyzers.Analyzers;
using Bang.Analyzers.CodeFixProviders;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace Bang.Analyzers.Tests.CodeFixProviders;

using Verify = BangCodeFixProviderVerifier<SystemAnalyzer, AddAttributeCodeFixProvider>;

[TestClass]
public sealed class AddAttributeCodeFixProviderTests
{
    [TestMethod(displayName: "All systems need a Filter annotation.")]
    public async Task SystemsNotAnnotatedWithTheFilterAttribute()
    {
        const string source = @"
using System.Collections.Immutable;
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public class System : IUpdateSystem
{
    public void Update(Context context) { }
};";

        const string codeFix = @"
using System.Collections.Immutable;
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

[Filter]
public class System : IUpdateSystem
{
    public void Update(Context context) { }
};";

        var expected = Verify.Diagnostic(SystemAnalyzer.FilterAttribute)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(11, 14, 11, 20);

        await Verify.VerifyCodeFixAsync(source, expected, codeFix);
    }
    [TestMethod(displayName: "The codefix adds the namespace if needed.")]
    public async Task SystemsNotAnnotatedWithTheFilterAttributeAndWithNoNamespace()
    {
        const string source = @"
using System.Collections.Immutable;
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;

namespace BangAnalyzerTestNamespace;

public class System : Bang.Systems.IUpdateSystem
{
    public void Update(Context context) { }
};";

        const string codeFix = @"
using System.Collections.Immutable;
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

[Filter]
public class System : Bang.Systems.IUpdateSystem
{
    public void Update(Context context) { }
};";

        var expected = Verify.Diagnostic(SystemAnalyzer.FilterAttribute)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(10, 14, 10, 20);

        await Verify.VerifyCodeFixAsync(source, expected, codeFix);
    }

    [TestMethod(displayName: "Messager systems must be annotated with the Messager attribute.")]
    public async Task NonAnnotatedMessagerSystems()
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
public class IncorrectMessagerSystem : IMessagerSystem
{
    public void OnMessage(World world, Entity entity, IMessage message) { }
}";
        const string codeFix = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public readonly record struct CorrectMessage : IMessage;

[Messager]
[Filter(ContextAccessorKind.Read, typeof(CorrectMessage))]
public class IncorrectMessagerSystem : IMessagerSystem
{
    public void OnMessage(World world, Entity entity, IMessage message) { }
}";

        var expected = Verify.Diagnostic(SystemAnalyzer.MessagerAttribute)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(13, 14, 13, 37);

        await Verify.VerifyCodeFixAsync(source, expected, codeFix);
    }

    [TestMethod(displayName: "Reactive systems must be annotated with the Watch attribute.")]
    public async Task NonAnnotatedReactiveSystems()
    {
        const string source = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;
using System.Collections.Immutable;

namespace BangAnalyzerTestNamespace;

public readonly record struct CorrectComponent : IComponent;

[Filter(ContextAccessorKind.Read, typeof(CorrectComponent))]
public class IncorrectReactiveSystem : IReactiveSystem
{
    public void OnAdded(World world, ImmutableArray<Entity> entities) { }
    public void OnRemoved(World world, ImmutableArray<Entity> entities) { }
    public void OnModified(World world, ImmutableArray<Entity> entities) { }
}";
        const string codeFix = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;
using System.Collections.Immutable;

namespace BangAnalyzerTestNamespace;

public readonly record struct CorrectComponent : IComponent;

[Watch]
[Filter(ContextAccessorKind.Read, typeof(CorrectComponent))]
public class IncorrectReactiveSystem : IReactiveSystem
{
    public void OnAdded(World world, ImmutableArray<Entity> entities) { }
    public void OnRemoved(World world, ImmutableArray<Entity> entities) { }
    public void OnModified(World world, ImmutableArray<Entity> entities) { }
}";

        var expected = Verify.Diagnostic(SystemAnalyzer.WatchAttribute)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(14, 14, 14, 37);

        await Verify.VerifyCodeFixAsync(source, expected, codeFix);
    }
}