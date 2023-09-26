using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bang.Analyzers.CodeFixProviders;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddAttributeCodeFixProvider)), Shared]
public sealed class AddAttributeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(
            Diagnostics.Systems.FilterAttribute.Id,
            Diagnostics.Systems.WatchAttribute.Id,
            Diagnostics.Systems.MessagerAttribute.Id
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
            var attributeToCreate = diagnostic.Id switch
            {
                Diagnostics.Systems.WatchAttribute.Id => "Watch",
                Diagnostics.Systems.MessagerAttribute.Id => "Messager",
                _ => "Filter"
            };

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            if (root.FindNode(diagnosticSpan) is not TypeDeclarationSyntax typeDeclarationSyntax)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixes.AddAttribute.Title(attributeToCreate),
                    createChangedDocument: c => AddAttribute(context.Document, typeDeclarationSyntax, attributeToCreate, c),
                    equivalenceKey: nameof(CodeFixes.AddAttribute)),
                diagnostic
            );
        }
    }

    private async Task<Document> AddAttribute(
        Document document,
        TypeDeclarationSyntax typeDeclarationSyntax,
        string attributeToCreate,
        CancellationToken cancellationToken)
    {
        var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (oldRoot is null)
            return document;

        var nameSyntax = SyntaxFactory.IdentifierName(attributeToCreate);
        var attribute = SyntaxFactory.Attribute(nameSyntax);
        var separatedSyntaxList = SyntaxFactory.SeparatedList(new[] { attribute });
        var newAttributeList = SyntaxFactory.AttributeList(separatedSyntaxList);
        var attributeLists =
            new SyntaxList<AttributeListSyntax>(typeDeclarationSyntax.AttributeLists.Prepend(newAttributeList));
        var newTypeDeclarationSyntax = typeDeclarationSyntax.WithAttributeLists(attributeLists);
        var rootWithAttribute = oldRoot.ReplaceNode(typeDeclarationSyntax, newTypeDeclarationSyntax);

        var bangSystemsIsImported = rootWithAttribute
            .ChildNodes()
            .OfType<UsingDirectiveSyntax>()
            .Any(u => u.Name.ToString() == "Bang.Systems");

        if (bangSystemsIsImported)
            return document.WithSyntaxRoot(rootWithAttribute);

        if (rootWithAttribute is not CompilationUnitSyntax compilationUnit)
            return document.WithSyntaxRoot(rootWithAttribute);

        var bangToken = SyntaxFactory.IdentifierName("Bang");
        var systemsToken = SyntaxFactory.IdentifierName("Systems");
        var name = SyntaxFactory.QualifiedName(bangToken, systemsToken);
        var rootWithImportedNamespace = compilationUnit.AddUsings(SyntaxFactory.UsingDirective(name));

        return document.WithSyntaxRoot(rootWithImportedNamespace);
    }
}
