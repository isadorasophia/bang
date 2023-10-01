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
        ReferenceAssemblies = Net.Net80;
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
        ReferenceAssemblies = Net.Net80;
    }
}

/// <summary>
/// This is kind of a hack because, as of now, net7.0 is not available out-of-the-box for analyzer testing.
/// Delete once this is not longer the case.
/// TODO: Update nuget packages once net8.0 is actually released.
/// </summary>
internal static class Net
{
    private static readonly Lazy<ReferenceAssemblies> _lazyNet80 = new(() =>
        new ReferenceAssemblies(
            "net8.0",
            new PackageIdentity(
                "Microsoft.NETCore.App.Ref",
                "8.0.0-rc.1.23419.4"),
            Path.Combine("ref", "net8.0")
        )
    );
    public static ReferenceAssemblies Net80 => _lazyNet80.Value;

    private static readonly Lazy<ReferenceAssemblies> _lazyNet80Windows = new(() =>
        Net80.AddPackages(
            ImmutableArray.Create(
                new PackageIdentity("Microsoft.WindowsDesktop.App.Ref", "8.0.0-rc.1.23420.5"))));
    public static ReferenceAssemblies Net80Windows => _lazyNet80Windows.Value;
}