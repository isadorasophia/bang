using Bang.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Bang.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MessageAnalyzer : BaseComponentAnalyzer
{
    public static readonly DiagnosticDescriptor ClassesCannotBeMessages = new(
        id: Diagnostics.Messages.ClassesCannotBeMessages.Id,
        title: nameof(MessageAnalyzer) + "." + nameof(ClassesCannotBeMessages),
        messageFormat: Diagnostics.Messages.ClassesCannotBeMessages.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All Bang Messages should be declared as readonly structs."
    );

    public static readonly DiagnosticDescriptor MessagesMustBeReadonly = new(
        id: Diagnostics.Messages.MessagesMustBeReadonly.Id,
        title: nameof(MessageAnalyzer) + "." + nameof(MessagesMustBeReadonly),
        messageFormat: Diagnostics.Messages.MessagesMustBeReadonly.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All Bang Messages should be declared as readonly structs."
    );

    public static readonly DiagnosticDescriptor MessagesCannotBeInteractions = new(
        id: Diagnostics.Messages.MessagesCannotBeInteractions.Id,
        title: nameof(ComponentAnalyzer) + "." + nameof(MessagesCannotBeInteractions),
        messageFormat: Diagnostics.Messages.MessagesCannotBeInteractions.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Structs implementing IMessage cannot also implement IInteraction."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(ClassesCannotBeMessages, MessagesMustBeReadonly, MessagesCannotBeInteractions);


    protected override string InterfaceName => TypeMetadataNames.MessageInterface;

    protected override DiagnosticDescriptor DoNotUseClassesDiagnostic => ClassesCannotBeMessages;

    protected override DiagnosticDescriptor ReadonlyDiagnostic => MessagesMustBeReadonly;

    protected override void PerformExtraAnalysis(SyntaxNodeAnalysisContext context, INamedTypeSymbol typeSymbol, Location diagnosticLocation)
    {
        // Bail if IInteraction is not resolvable.
        var interactionInterface = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.InteractionInterface);
        if (interactionInterface is null)
            return;

        // The base class already checked that we do implement IComponent 
        if (typeSymbol.ImplementsInterface(interactionInterface))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(MessagesCannotBeInteractions, diagnosticLocation)
            );
        }
    }
}