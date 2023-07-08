using System.Collections.Immutable;
using Bang.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Bang.Analyzers.Tests;

/// <summary>
/// Verifier that includes Bang binaries to the project.
/// </summary>
/// <typeparam name="TAnalyzer">Analyzer under test.</typeparam>
public class BangAnalyzerVerifier<TAnalyzer> : AnalyzerVerifier<TAnalyzer, BangTest<TAnalyzer>, XUnitVerifier>
	where TAnalyzer : DiagnosticAnalyzer, new() { }
	
/// <summary>
/// Implementation of CSharpAnalyzerTest that uses net7.0 (not enabled by default
/// as of now and needed for Bang) and includes the Bang dlls.
/// </summary>
/// <typeparam name="TAnalyzer">Analyzer under test.</typeparam>
public sealed class BangTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, XUnitVerifier>
	where TAnalyzer : DiagnosticAnalyzer, new()
{
	public BangTest()
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