using Bang.Analyzers.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bang.Analyzers.Tests.Analyzers;

using Verify = BangAnalyzerVerifier<WorldAnalyzer>;

[TestClass]
public sealed class WorldAnalyzerTests
{
    [TestMethod(displayName: "Correctly annotated components do not trigger the analyzer.")]
    public async Task CorrectlyAnnotatedSystemsDoNotTriggerTheAnalyzer()
    {
        const string source = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;
using System;
using System.Collections.Immutable;

namespace BangAnalyzerTestNamespace;

[Unique]
public readonly record struct UniqueComponent : IComponent;

[Filter(ContextAccessorKind.Read, typeof(UniqueComponent))]
public sealed class CorrectSystem : IFixedUpdateSystem
{
    public void FixedUpdate(Context context)
    {
        var uniqueEntity = context.World.GetUniqueEntity<UniqueComponent>();
        Console.WriteLine(uniqueEntity);
    }
}";
        await Verify.VerifyAnalyzerAsync(source);
    }

    [TestMethod(displayName: "This call should not trigger on other generic methods.")]
    public async Task NoTriggerOnOtherGenericMethods()
    {
        const string source = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;
using System;
using System.Linq;
using System.Collections.Immutable;

namespace BangAnalyzerTestNamespace;

[Unique]
public readonly record struct UniqueComponent : IComponent;

[Filter(ContextAccessorKind.Read, typeof(UniqueComponent))]
public sealed class CorrectSystem : IFixedUpdateSystem
{
    public void FixedUpdate(Context context)
    {
        var list = ImmutableList.Create<int>(1, 2, 3);
        var first = list.First<int>();
        System.Console.WriteLine(first);
    }
}";
        await Verify.VerifyAnalyzerAsync(source);
    }

    [TestMethod(displayName: "Calling get unique entity with a non-unique component fails.")]
    public async Task GetUniqueEntityWithANonUniqueComponent()
    {
        const string source = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;
using System;
using System.Collections.Immutable;

namespace BangAnalyzerTestNamespace;

public readonly record struct UniqueComponent : IComponent;

[Filter(ContextAccessorKind.Read, typeof(UniqueComponent))]
public sealed class CorrectSystem : IFixedUpdateSystem
{
    public void FixedUpdate(Context context)
    {
        var uniqueEntity = context.World.GetUniqueEntity<UniqueComponent>();
        Console.WriteLine(uniqueEntity);
    }
}";
        var expected = Verify.Diagnostic(WorldAnalyzer.MarkUniqueComponentAsUnique)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithSpan(19, 58, 19, 73);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }

    [TestMethod(displayName: "Calling get unique entity with a non-unique component fails when world is a variable.")]
    public async Task GetUniqueEntityWithANonUniqueComponentWorldVar()
    {
        const string source = @"
using Bang;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;
using System;
using System.Collections.Immutable;

namespace BangAnalyzerTestNamespace;

public readonly record struct UniqueComponent : IComponent;

[Filter(ContextAccessorKind.Read, typeof(UniqueComponent))]
public sealed class CorrectSystem : IFixedUpdateSystem
{
    public void FixedUpdate(Context context)
    {
        var world = context.World;
        var uniqueComponent = world.GetUnique<UniqueComponent>();
        Console.WriteLine(uniqueComponent);
    }
}";
        var expected = Verify.Diagnostic(WorldAnalyzer.MarkUniqueComponentAsUnique)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithSpan(20, 47, 20, 62);

        await Verify.VerifyAnalyzerAsync(source, expected);
    }
}