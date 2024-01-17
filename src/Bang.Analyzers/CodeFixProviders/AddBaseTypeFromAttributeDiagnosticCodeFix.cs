using Bang.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Composition;

namespace Bang.Analyzers.CodeFixProviders;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddBaseTypeFromAttributeDiagnosticCodeFix)), Shared]
public sealed class AddBaseTypeFromAttributeDiagnosticCodeFix : AddBaseTypeCodeFix
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(
            Diagnostics.Attributes.NonApplicableUniqueAttribute.Id
        );

    protected override TypeDeclarationSyntax? FindTypeDeclarationSyntaxFromDiagnosticSpan(
        SyntaxNode root,
        TextSpan diagnosticSpan
    )
    {
        if (root.FindNode(diagnosticSpan) is not AttributeSyntax attributeSyntax)
            return null;

        return attributeSyntax.GetTypeAnnotatedByAttribute() as TypeDeclarationSyntax;
    }

    protected override IEnumerable<BaseTypeSuggestion> GetBaseTypeSuggestions(TypeDeclarationSyntax typeDeclarationSyntax, INamedTypeSymbol namedTypeSymbol)
    {
        // If the type being annotated is a value type, we should suggest making it an interaction or a component.
        // Otherwise, it can only be helped by being made a StateMachine, which can only be a class.
        if (!typeDeclarationSyntax.IsValueType(namedTypeSymbol))
        {
            // Records can't inherit State machine
            // We don't want to suggest changing the base type.
            if (typeDeclarationSyntax is RecordDeclarationSyntax || namedTypeSymbol.HasANonObjectBaseType())
                return Enumerable.Empty<BaseTypeSuggestion>();

            return [BaseTypeSuggestion.StateMachine];
        }

        var typeName = namedTypeSymbol.Name;

        // We can use some heuristics to determine the user's intentions.
        return
            typeName.EndsWith("Component") ? [BaseTypeSuggestion.IComponent] :
            typeName.EndsWith("Interaction") ? [BaseTypeSuggestion.IInteraction] :
            [BaseTypeSuggestion.IComponent, BaseTypeSuggestion.IInteraction];
    }
}