using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Bang.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InteractionAnalyzer : BaseComponentAnalyzer
{
    public static readonly DiagnosticDescriptor ClassesCannotBeInteractions = new(
        id: Diagnostics.Interactions.ClassesCannotBeInteractions.Id,
        title: nameof(InteractionAnalyzer) + "." + nameof(ClassesCannotBeInteractions),
        messageFormat: Diagnostics.Interactions.ClassesCannotBeInteractions.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All Bang Interactions should be declared as readonly structs."
    );

    public static readonly DiagnosticDescriptor InteractionsMustBeReadonly = new(
        id: Diagnostics.Interactions.InteractionsMustBeReadonly.Id,
        title: nameof(InteractionAnalyzer) + "." + nameof(InteractionsMustBeReadonly),
        messageFormat: Diagnostics.Interactions.InteractionsMustBeReadonly.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All Bang Interactions should be declared as readonly structs."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(ClassesCannotBeInteractions, InteractionsMustBeReadonly);


    protected override string InterfaceName => TypeMetadataNames.InteractionInterface;

    protected override DiagnosticDescriptor DoNotUseClassesDiagnostic => ClassesCannotBeInteractions;

    protected override DiagnosticDescriptor ReadonlyDiagnostic => InteractionsMustBeReadonly;
}