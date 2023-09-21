using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Bang.Analyzers.Extensions;

namespace Bang.Analyzers.Analyzers;

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

    public static readonly DiagnosticDescriptor ComponentsCannotBeMessages = new(
        id: Diagnostics.Components.ComponentsCannotBeMessages.Id,
        title: nameof(ComponentAnalyzer) + "." + nameof(ComponentsCannotBeMessages),
        messageFormat: Diagnostics.Components.ComponentsCannotBeMessages.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Structs implementing IComponent cannot also implement IMessage."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(ClassesCannotBeComponents, ComponentsMustBeReadonly, ComponentsCannotBeMessages);

    protected override string InterfaceName => TypeMetadataNames.ComponentInterface;

    protected override DiagnosticDescriptor DoNotUseClassesDiagnostic => ClassesCannotBeComponents;

    protected override DiagnosticDescriptor ReadonlyDiagnostic => ComponentsMustBeReadonly;

    protected override void PerformExtraAnalysis(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol typeSymbol,
        Location diagnosticLocation
    )
    {
        // Bail if IMessage is not resolvable.
        var messageInterface = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.MessageInterface);
        if (messageInterface is null)
            return;

        // The base class already checked that we do implement IComponent 
        if (typeSymbol.ImplementsInterface(messageInterface))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(ComponentsCannotBeMessages, diagnosticLocation)
            );

        }
    }
}