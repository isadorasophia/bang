using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bang.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComponentAnalyzer : BaseComponentAnalyzer
{
    public static readonly DiagnosticDescriptor ClassesCannotBeComponents = new(
        id: Diagnostics.Components.ClassesCannotBeComponents.Id,
        title: nameof(ComponentAnalyzer) + "." + nameof(ClassesCannotBeComponents),
        messageFormat: Diagnostics.Components.ClassesCannotBeComponents.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All Bang Components should be declared as readonly structs."
    );

    public static readonly DiagnosticDescriptor ComponentsMustBeReadonly = new(
        id: Diagnostics.Components.ComponentsMustBeReadonly.Id,
        title: nameof(ComponentAnalyzer) + "." + nameof(ComponentsMustBeReadonly),
        messageFormat: Diagnostics.Components.ComponentsMustBeReadonly.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All Bang Components should be declared as readonly structs."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(ClassesCannotBeComponents, ComponentsMustBeReadonly);

    protected override string InterfaceName => TypeMetadataNames.ComponentInterface;

    protected override DiagnosticDescriptor DoNotUseClassesDiagnostic => ClassesCannotBeComponents;

    protected override DiagnosticDescriptor ReadonlyDiagnostic => ComponentsMustBeReadonly;
}