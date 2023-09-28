using Bang.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;

namespace Bang.Analyzers.CodeFixProviders;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveAttributeCodeFixProvider)), Shared]
public sealed class RemoveAttributeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(
            Diagnostics.Systems.NonApplicableWatchAttribute.Id,
            Diagnostics.Systems.NonApplicableMessagerAttribute.Id
        );

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document
            .GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (root is null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            if (root.FindNode(diagnosticSpan) is not AttributeSyntax attributeSyntax)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixes.RemoveAttribute.Title,
                    createChangedDocument: c => RemoveAttribute(context.Document, attributeSyntax, c),
                    equivalenceKey: nameof(CodeFixes.RemoveAttribute)),
                diagnostic
            );
        }
    }

    private async Task<Document> RemoveAttribute(
        Document document,
        AttributeSyntax attributeSyntax,
        CancellationToken cancellationToken
    )
    {
        if (attributeSyntax.Parent is not AttributeListSyntax attributeList)
            return document;

        var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (oldRoot is null)
            return document;

        if (attributeList.Attributes.Count == 1)
        {
            // Remove the whole attribute list.
            var newRoot = oldRoot.RemoveNode(attributeList, SyntaxRemoveOptions.KeepLeadingTrivia);
            return document.WithSyntaxRoot(newRoot!);
        }
        else
        {
            // Simply remove the offending attribute.
            var newRoot = oldRoot.RemoveNode(attributeSyntax, SyntaxRemoveOptions.KeepNoTrivia);
            return document.WithSyntaxRoot(newRoot!);
        }
    }
}