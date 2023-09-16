using System.Collections.Immutable;
using Bang.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bang.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AttributeAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor NonComponentsOnFilterAttribute = new(
        id: Diagnostics.Attributes.NonComponentsOnFilterAttribute.Id,
        title: nameof(AttributeAnalyzer) + "." + nameof(NonComponentsOnFilterAttribute),
        messageFormat: Diagnostics.Attributes.NonComponentsOnFilterAttribute.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Types in Filter attribute must be IComponents."
    );

    public static readonly DiagnosticDescriptor NonMessagesOnMessagerFilterAttribute = new(
        id: Diagnostics.Attributes.NonMessagesOnMessagerFilterAttribute.Id,
        title: nameof(AttributeAnalyzer) + "." + nameof(NonMessagesOnMessagerFilterAttribute),
        messageFormat: Diagnostics.Attributes.NonMessagesOnMessagerFilterAttribute.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Types in Filter attribute for IWatchComponents must be IMessages."
    );

    public static readonly DiagnosticDescriptor NonMessagesOnMessagerAttribute = new(
        id: Diagnostics.Attributes.NonMessagesOnMessagerAttribute.Id,
        title: nameof(AttributeAnalyzer) + "." + nameof(NonMessagesOnMessagerAttribute),
        messageFormat: Diagnostics.Attributes.NonMessagesOnMessagerAttribute.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Types in Messager attribute must be IMessages."
    );

    public static readonly DiagnosticDescriptor NonComponentsOnWatchAttribute = new(
        id: Diagnostics.Attributes.NonComponentsOnWatchAttribute.Id,
        title: nameof(AttributeAnalyzer) + "." + nameof(NonComponentsOnWatchAttribute),
        messageFormat: Diagnostics.Attributes.NonComponentsOnWatchAttribute.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Types in Watch attribute must be IComponents."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        NonComponentsOnFilterAttribute,
        NonMessagesOnMessagerFilterAttribute,
        NonMessagesOnMessagerAttribute,
        NonComponentsOnWatchAttribute
    );

    public override void Initialize(AnalysisContext context)
    {
        var syntaxKind = ImmutableArray.Create(SyntaxKind.AttributeArgumentList);

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction(Analyze, syntaxKind);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        // Bail if wrong syntax
        if (context.Node is not AttributeArgumentListSyntax argumentList)
            return;

        var attributeData = GetAttributeDataForArgumentList(context, argumentList);
        var attributeClass = attributeData?.AttributeClass;
        if (attributeClass is null)
            return;

        var diagnosticInfo = GetInterfaceThatMustBeImplemented(context, attributeClass);
        if (diagnosticInfo is null)
            return;

        var (interfaceThatMustBeImplemented, diagnosticDescriptor) = diagnosticInfo.Value;

        var offendingArguments = argumentList.Arguments
            .Where(argument =>
            {
                // If the argument is not a type of expression we don't need to check it.
                if (argument.Expression is not TypeOfExpressionSyntax typeOfExpression)
                    return false;

                if (context.SemanticModel.GetSymbolInfo(typeOfExpression.Type).Symbol is not ITypeSymbol typeSymbol)
                    return false;

                // If the type passed does not implement the relevant interface it's a violation.
                return !typeSymbol.ImplementsInterface(interfaceThatMustBeImplemented);
            });

        foreach (var offense in offendingArguments)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    diagnosticDescriptor,
                    offense.GetLocation()
                )
            );
        }
    }

    // First call to .Parent gets the AttributeList.
    // Second call to .Parent get the type annotated with the attribute we're looking for.
    public static SyntaxNode? GetTypeAnnotatedByAttribute(AttributeSyntax? attributeSyntax)
        => attributeSyntax?.Parent?.Parent;

    public static AttributeData? GetAttributeDataForArgumentList(
        SyntaxNodeAnalysisContext context,
        AttributeArgumentListSyntax argumentListSyntax
    )
    {
        var attributeSyntax = argumentListSyntax.Parent as AttributeSyntax;
        var annotatedTypeNode = GetTypeAnnotatedByAttribute(attributeSyntax);
        if (annotatedTypeNode is null)
            return null;

        return context.SemanticModel
            .GetDeclaredSymbol(annotatedTypeNode)?
            .GetAttributes()
            .Single(a => a.ApplicationSyntaxReference!.GetSyntax() == attributeSyntax);
    }

    private static (INamedTypeSymbol, DiagnosticDescriptor)? GetInterfaceThatMustBeImplemented(
        SyntaxNodeAnalysisContext context,
        ISymbol attributeClass
    )
    {
        // Checks for FilterAttribute
        var filterAttribute = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.FilterAttribute);
        if (attributeClass.Equals(filterAttribute, SymbolEqualityComparer.IncludeNullability))
        {
            var interfaceToResolve = TypeMetadataNames.ComponentInterface;
            var diagnosticDescriptor = NonComponentsOnFilterAttribute;

            var attributeSyntax = ((AttributeArgumentListSyntax)context.Node).Parent as AttributeSyntax;
            var typeAnnotatedByAttribute = GetTypeAnnotatedByAttribute(attributeSyntax);
            if (typeAnnotatedByAttribute is not null)
            {
                // Get symbol for the type annotated by the attribute we're analyzing
                if (context.SemanticModel.GetDeclaredSymbol(typeAnnotatedByAttribute) is ITypeSymbol typeSymbol)
                {
                    // Bail if we can't resolve the messager interface
                    var bangMessagerSystem = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.MessagerSystemInterface);
                    if (bangMessagerSystem is not null)
                    {
                        if (typeSymbol.ImplementsInterface(bangMessagerSystem))
                        {
                            // If the type annotated implements IMessagerSystem, it should only expect messages in its filter.
                            interfaceToResolve = TypeMetadataNames.MessageInterface;
                            diagnosticDescriptor = NonMessagesOnMessagerFilterAttribute;
                        }
                    }
                }
            }

            var interfaceToImplement = context.Compilation.GetTypeByMetadataName(interfaceToResolve);
            return interfaceToImplement is null ? null : (interfaceToImplement, diagnosticDescriptor);
        }

        // Checks for MessagerAttribute
        var messagerAttribute = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.MessagerAttribute);
        if (attributeClass.Equals(messagerAttribute, SymbolEqualityComparer.IncludeNullability))
        {
            var interfaceToImplement = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.MessageInterface);
            return interfaceToImplement is null ? null : (interfaceToImplement, NonMessagesOnMessagerAttribute);
        }

        // Checks for WatchAttribute
        var watchAttribute = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.WatchAttribute);
        if (attributeClass.Equals(watchAttribute, SymbolEqualityComparer.IncludeNullability))
        {
            var interfaceToImplement = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.ComponentInterface);
            return interfaceToImplement is null ? null : (interfaceToImplement, NonComponentsOnWatchAttribute);
        }

        return null;
    }
}