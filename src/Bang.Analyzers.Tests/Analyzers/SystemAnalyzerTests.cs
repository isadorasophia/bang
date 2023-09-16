using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bang.Analyzers.Tests;

using Verify = BangAnalyzerVerifier<SystemAnalyzer>;

[TestClass]
public sealed class SystemAnalyzerTests
{
    private const string ISystemWithProperAnnotationSourceCode = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public readonly record struct CorrectComponent : IComponent;

[Filter(ContextAccessorKind.Read, typeof(CorrectComponent))]
public class CorrectSystem : ISystem
{
}";
    private const string IReactiveSystemWithProperAnnotationSourceCode = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;
using System.Collections.Immutable;

namespace BangAnalyzerTestNamespace;

public readonly record struct CorrectComponent : IComponent;

[Watch(typeof(CorrectComponent))]
[Filter(ContextAccessorKind.Read, typeof(CorrectComponent))]
public class CorrectReactiveSystem : IReactiveSystem
{
    public void OnAdded(World world, ImmutableArray<Entity> entities) { }
    public void OnRemoved(World world, ImmutableArray<Entity> entities) { }
    public void OnModified(World world, ImmutableArray<Entity> entities) { }
}";
    private const string IMessagerSystemWithProperAnnotationSourceCode = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;
public readonly record struct CorrectMessage : IMessage;

[Messager(typeof(CorrectMessage))]
[Filter(ContextAccessorKind.Read, typeof(CorrectMessage))]
public class CorrectMessagerSystem : IMessagerSystem
{
    public void OnMessage(World world, Entity entity, IMessage message) { }
}";

    [TestMethod(displayName: "Properly annotated systems do not trigger the analyzer.")]
    [DataRow(ISystemWithProperAnnotationSourceCode)]
    [DataRow(IReactiveSystemWithProperAnnotationSourceCode)]
    [DataRow(IMessagerSystemWithProperAnnotationSourceCode)]
    public async Task ProperlyAnnotatedSystemsDoNotTriggerTheAnalyzer(string source)
    {
        await Verify.VerifyAnalyzerAsync(source);
    }

    private const string ISystemWithoutFilterAnnotationSourceCode = @"
using System.Collections.Immutable;
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public class System : ISystem
{    
}";

    private const string IExitSystemWithoutFilterAnnotationSourceCode = @"
using System.Collections.Immutable;
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public class ExitSystem : IExitSystem
{
    public void Exit(Context context) { }
}";

    private const string IFixedUpdateSystemWithoutFilterAnnotationSourceCode = @"
using System.Collections.Immutable;
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public class FixedUpdateSystem : IFixedUpdateSystem
{
    public void FixedUpdate(Context context) { }
}";

    private const string IMessagerSystemWithoutFilterAnnotationSourceCode = @"
using System.Collections.Immutable;
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public readonly record struct CorrectMessage : IMessage;

[Messager(typeof(CorrectMessage))]
public class MessagerSystem : IMessagerSystem
{
    public void OnMessage(World world, Entity entity, IMessage message) { }
}";

    private const string IReactiveSystemWithoutFilterAnnotationSourceCode = @"
using System.Collections.Immutable;
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public readonly record struct CorrectComponent : IComponent;

[Watch(typeof(CorrectComponent))]
public class ReactiveSystem : IReactiveSystem
{
    public void OnAdded(World world, ImmutableArray<Entity> entities) { }
    public void OnModified(World world, ImmutableArray<Entity> entities) { }
    public void OnRemoved(World world, ImmutableArray<Entity> entities) { }
}";

    private const string IRenderSystemWithoutFilterAnnotationSourceCode = @"
using System.Collections.Immutable;
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public class RenderSystem : IRenderSystem 
{
}";

    private const string IStartupSystemWithoutFilterAnnotationSourceCode = @"
using System.Collections.Immutable;
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public class StartupSystem : IStartupSystem
{
    public void Start(Context context) { }
}";

    private const string IUpdateSystemWithoutFilterAnnotationSourceCode = @"
using System.Collections.Immutable;
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;

namespace BangAnalyzerTestNamespace;

public class UpdateSystem : IUpdateSystem
{
    public void Update(Context context) { }
}";

    [TestMethod(displayName: "All systems need a Filter annotation.")]
    [DataRow(ISystemWithoutFilterAnnotationSourceCode)]
    [DataRow(IExitSystemWithoutFilterAnnotationSourceCode)]
    [DataRow(IFixedUpdateSystemWithoutFilterAnnotationSourceCode)]
    [DataRow(IRenderSystemWithoutFilterAnnotationSourceCode)]
    [DataRow(IStartupSystemWithoutFilterAnnotationSourceCode)]
    [DataRow(IUpdateSystemWithoutFilterAnnotationSourceCode)]
    public async Task SystemsNotAnnotatedWithTheFilterAttributeTriggerTheAnalyzer(string source)
    {
        // Figure out starting index of diagnosis (class declaration except when an annotation is used)
        var diagnosisStartPoint = source.IndexOf("[") > 0 ? source.IndexOf("[") : source.IndexOf("public class");
        // Count newlines to figure out the starting line. Add 1 because lines are 1-indexed.
        var startLine = Regex.Count(source.Substring(0, diagnosisStartPoint), Environment.NewLine) + 1;
        var endLine = Regex.Count(source.Substring(diagnosisStartPoint, source.Length - diagnosisStartPoint - 1), Environment.NewLine);
        var expected = Verify.Diagnostic(SystemAnalyzer.FilterAttribute)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(startLine, 1, startLine + endLine, 2);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [DataRow(IReactiveSystemWithoutFilterAnnotationSourceCode)]
    [DataRow(IMessagerSystemWithoutFilterAnnotationSourceCode)]
    public async Task ReactiveAndMessagerSystemsNotAnnotatedWithTheFilterAttributeDoNotTriggerTheAnalyzer(string source)
    {
        await Verify.VerifyAnalyzerAsync(source);
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

        var expected = Verify.Diagnostic(SystemAnalyzer.MessagerAttribute)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(12, 1, 16, 2);

        await Verify.VerifyAnalyzerAsync(source, expected);
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

        var expected = Verify.Diagnostic(SystemAnalyzer.WatchAttribute)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(13, 1, 19, 2);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

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

        var expected = Verify.Diagnostic(SystemAnalyzer.NonApplicableMessagerAttribute)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithSpan(12, 1, 16, 2);

        await Verify.VerifyAnalyzerAsync(source, expected);
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

        var expected = Verify.Diagnostic(SystemAnalyzer.NonApplicableWatchAttribute)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithSpan(12, 1, 16, 2);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }
}