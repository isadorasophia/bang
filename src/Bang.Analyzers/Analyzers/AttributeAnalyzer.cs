using Bang.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Bang.Analyzers.Analyzers;

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

    public static readonly DiagnosticDescriptor NonApplicableUniqueAttribute = new(
        id: Diagnostics.Attributes.NonApplicableUniqueAttribute.Id,
        title: nameof(AttributeAnalyzer) + "." + nameof(NonApplicableUniqueAttribute),
        messageFormat: Diagnostics.Attributes.NonApplicableUniqueAttribute.Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Unique attribute must annotate only IComponents, StateMachines and IInteractions."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        NonComponentsOnFilterAttribute,
        NonMessagesOnMessagerAttribute,
        NonComponentsOnWatchAttribute,
        NonApplicableUniqueAttribute
    );

    public override void Initialize(AnalysisContext context)
    {
        var syntaxKind = ImmutableArray.Create(
            SyntaxKind.Attribute,
            SyntaxKind.AttributeArgumentList
        );

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction(Analyze, syntaxKind);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        AnalyzeAttributeArguments(context);
        AnalyzeTypeBeingAnnotated(context);
    }

    private static void AnalyzeAttributeArguments(SyntaxNodeAnalysisContext context)
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

        var offendingArguments = argumentList.Arguments.Where(argument =>
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

    private static void AnalyzeTypeBeingAnnotated(SyntaxNodeAnalysisContext context)
    {
        // Bail if wrong syntax
        if (context.Node is not AttributeSyntax attributeSyntax)
            return;

        // Bail if we can't find the unique attribute.
        var uniqueAttribute = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.UniqueAttribute);
        if (uniqueAttribute is null)
            return;

        // Bail if we can't find the IComponent interface.
        var componentInterface = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.ComponentInterface);
        if (componentInterface is null)
            return;

        // Bail if we can't find the IComponent interface.
        var interactionInterface = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.InteractionInterface);
        if (interactionInterface is null)
            return;

        // Bail if we can't find the StateMachine base class.
        var stateMachineBaseClass = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.StateMachineClass);
        if (stateMachineBaseClass is null)
            return;

        var annotatedTypeNode = attributeSyntax.GetTypeAnnotatedByAttribute();
        if (annotatedTypeNode is null)
            return;

        var annotatedType = context.SemanticModel.GetDeclaredSymbol(annotatedTypeNode);
        if (annotatedType is not ITypeSymbol annotatedTypeSymbol)
            return;

        var attributeData = annotatedType
            .GetAttributes()
            .SingleOrDefault(a => a.ApplicationSyntaxReference!.GetSyntax() == attributeSyntax);
        var attributeClass = attributeData?.AttributeClass;
        if (attributeClass is null)
            return;

        // Return if this is not an UniqueAttribute.
        if (!uniqueAttribute.Equals(attributeClass, SymbolEqualityComparer.IncludeNullability))
            return;

        if (!annotatedTypeSymbol.ImplementsInterface(componentInterface) &&
            !annotatedTypeSymbol.IsSubtypeOf(stateMachineBaseClass) &&
            !annotatedTypeSymbol.ImplementsInterface(interactionInterface))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    NonApplicableUniqueAttribute,
                    attributeSyntax.GetLocation()
                )
            );
        }
    }

    private static AttributeData? GetAttributeDataForArgumentList(
        SyntaxNodeAnalysisContext context,
        AttributeArgumentListSyntax argumentListSyntax
    )
    {
        var attributeSyntax = argumentListSyntax.Parent as AttributeSyntax;
        var annotatedTypeNode = attributeSyntax.GetTypeAnnotatedByAttribute();
        if (annotatedTypeNode is null)
            return null;

        return context.SemanticModel
            .GetDeclaredSymbol(annotatedTypeNode)?
            .GetAttributes()
            .SingleOrDefault(a => a.ApplicationSyntaxReference!.GetSyntax() == attributeSyntax);
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
            var interfaceToImplement = context.Compilation.GetTypeByMetadataName(TypeMetadataNames.ComponentInterface);
            return interfaceToImplement is null ? null : (interfaceToImplement, NonComponentsOnFilterAttribute);
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