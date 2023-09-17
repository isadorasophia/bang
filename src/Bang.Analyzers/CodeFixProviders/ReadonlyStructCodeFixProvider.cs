using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;

namespace Bang.Analyzers.Fixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReadonlyStructCodeFixProvider)), Shared]
public class ReadonlyStructCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(
            Diagnostics.Components.ComponentsMustBeReadonly.Id,
            Diagnostics.Messages.MessagesMustBeReadonly.Id
        );

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document
            .GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (root is null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            if (root.FindToken(diagnosticSpan.Start).Parent is not TypeDeclarationSyntax typeDeclaration)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixes.ReadonlyStruct.Title,
                    createChangedDocument: c => MakeStructReadonly(context.Document, typeDeclaration, c),
                    equivalenceKey: nameof(CodeFixes.ReadonlyStruct)),
                diagnostic
            );
        }
    }

    private static async Task<Document> MakeStructReadonly(
        Document document,
        TypeDeclarationSyntax recordDeclarationSyntax,
        CancellationToken cancellationToken
    )
    {
        // Add a readonly modifier to the struct.
        var readonlyToken = SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
        var readonlyStructDeclarationSyntax = recordDeclarationSyntax.AddModifiers(readonlyToken);

        // Replace the old struct declaration with the new, readonly, one.
        var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (oldRoot is null)
            return document;

        var newRoot = oldRoot.ReplaceNode(recordDeclarationSyntax, readonlyStructDeclarationSyntax);
        return document.WithSyntaxRoot(newRoot);
    }
}