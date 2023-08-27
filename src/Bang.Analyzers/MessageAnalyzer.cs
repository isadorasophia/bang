using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bang.Analyzers;

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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(ClassesCannotBeMessages, MessagesMustBeReadonly);


    protected override string InterfaceName => TypeMetadataNames.MessageInterface;

    protected override DiagnosticDescriptor DoNotUseClassesDiagnostic => ClassesCannotBeMessages;

    protected override DiagnosticDescriptor ReadonlyDiagnostic => MessagesMustBeReadonly;
}