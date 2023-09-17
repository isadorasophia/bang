using Bang.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Collections.Immutable;

namespace Bang.Analyzers.Tests;

/// <summary>
/// Verifier that includes Bang binaries to the project.
/// </summary>
/// <typeparam name="TAnalyzer">Analyzer under test.</typeparam>
public class BangAnalyzerVerifier<TAnalyzer> : AnalyzerVerifier<TAnalyzer, BangAnalyzerTest<TAnalyzer>, MSTestVerifier>
    where TAnalyzer : DiagnosticAnalyzer, new()
{ }

/// <summary>
/// Implementation of CSharpAnalyzerTest that uses net7.0 (not enabled by default
/// as of now and needed for Bang) and includes the Bang dlls.
/// </summary>
/// <typeparam name="TAnalyzer">Analyzer under test.</typeparam>
public sealed class BangAnalyzerTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, MSTestVerifier>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public BangAnalyzerTest()
    {
        var bangReference = MetadataReference.CreateFromFile(typeof(IComponent).Assembly.Location);
        TestState.AdditionalReferences.Add(bangReference);
        ReferenceAssemblies = Net.Net70;
    }
}

/// <summary>
/// Verifier that includes Bang binaries to the project.
/// </summary>
/// <typeparam name="TCodeFix">CodeFixProvider under test.</typeparam>
/// <typeparam name="TAnalyzer">Analyzer under test.</typeparam>
public class BangCodeFixProviderVerifier<TAnalyzer, TCodeFix> :
    CodeFixVerifier<TAnalyzer, TCodeFix, BangCodeFixTest<TAnalyzer, TCodeFix>, MSTestVerifier>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{ }

/// <summary>
/// Implementation of CSharpCodeFixTest that uses net7.0 (not enabled by default
/// as of now and needed for Bang) and includes the Bang dlls.
/// </summary>
/// <typeparam name="TAnalyzer">Analyzer under test.</typeparam>
/// <typeparam name="TCodeFix">CodeFixProvider under test.</typeparam>
public sealed class BangCodeFixTest<TAnalyzer, TCodeFix> : CSharpCodeFixTest<TAnalyzer, TCodeFix, MSTestVerifier>
    where TCodeFix : CodeFixProvider, new()
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public BangCodeFixTest()
    {
        var bangReference = MetadataReference.CreateFromFile(typeof(IComponent).Assembly.Location);
        TestState.AdditionalReferences.Add(bangReference);
        ReferenceAssemblies = Net.Net70;
    }
}

/// <summary>
/// This is kind of a hack because, as of now, net7.0 is not available out-of-the-box for analyzer testing.
/// Delete once this is not longer the case.
/// </summary>
internal static class Net
{
    private static readonly Lazy<ReferenceAssemblies> _lazyNet70 = new(() =>
        new ReferenceAssemblies(
            "net7.0",
            new PackageIdentity(
                "Microsoft.NETCore.App.Ref",
                "7.0.8"),
            Path.Combine("ref", "net7.0")
        )
    );
    public static ReferenceAssemblies Net70 => _lazyNet70.Value;

    private static readonly Lazy<ReferenceAssemblies> _lazyNet70Windows = new(() =>
        Net70.AddPackages(
            ImmutableArray.Create(
                new PackageIdentity("Microsoft.WindowsDesktop.App.Ref", "7.0.0-preview.5.22302.5"))));
    public static ReferenceAssemblies Net70Windows => _lazyNet70Windows.Value;
}