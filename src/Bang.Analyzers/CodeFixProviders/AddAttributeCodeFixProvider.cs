using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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
            var attributeToAdd = diagnostic.Id switch
            {
                Diagnostics.Systems.WatchAttribute.Id => AttributeToAdd.Watch,
                Diagnostics.Systems.MessagerAttribute.Id => AttributeToAdd.Messager,
                _ => AttributeToAdd.Filter
            };

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            if (root.FindNode(diagnosticSpan) is not TypeDeclarationSyntax typeDeclarationSyntax)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixes.AddAttribute.Title(attributeToAdd.ToString()),
                    createChangedDocument: c => AddAttribute(context.Document, typeDeclarationSyntax, attributeToAdd, c),
                    equivalenceKey: nameof(CodeFixes.AddAttribute)),
                diagnostic
            );
        }
    }

    private async Task<Document> AddAttribute(
        Document document,
        TypeDeclarationSyntax typeDeclarationSyntax,
        AttributeToAdd attributeToAdd,
        CancellationToken cancellationToken)
    {
        var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (oldRoot is null)
            return document;

        var newAttributeList = CreateNewAttributeList(attributeToAdd);
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

        var bangToken = IdentifierName("Bang");
        var systemsToken = IdentifierName("Systems");
        var name = QualifiedName(bangToken, systemsToken);
        var rootWithImportedNamespace = compilationUnit.AddUsings(UsingDirective(name));

        return document.WithSyntaxRoot(rootWithImportedNamespace);
    }

    private static AttributeListSyntax CreateNewAttributeList(AttributeToAdd attributeToAdd)
    {
        var attribute = attributeToAdd switch
        {
            AttributeToAdd.Filter => CreateFilterAttribute(),
            AttributeToAdd.Watch => CreateWatchAttribute(),
            AttributeToAdd.Messager => CreateMessagerAttribute(),
            _ => throw new ArgumentOutOfRangeException(nameof(attributeToAdd), attributeToAdd, null)
        };
        var separatedSyntaxList = SeparatedList(new[] { attribute });
        return AttributeList(separatedSyntaxList);
    }

    private static AttributeSyntax CreateFilterAttribute()
    {
        var nameSyntax = IdentifierName("Filter");
        var filterAttributeArgument = AttributeArgument(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("ContextAccessorFilter"),
                Token(SyntaxKind.DotToken),
                IdentifierName("AllOf")
            )
        );
        var typeOfExpression =
            AttributeArgument(TypeOfExpression(ParseTypeName("")));
        var attributeArguments =
            AttributeArgumentList(SeparatedList(
                new[]
                {
                    filterAttributeArgument,
                    typeOfExpression
                }));

        return Attribute(nameSyntax).WithArgumentList(attributeArguments);
    }

    private static AttributeSyntax CreateWatchAttribute()
    {
        var nameSyntax = IdentifierName("Watch");
        var typeOfExpression =
            AttributeArgument(TypeOfExpression(ParseTypeName("")));
        var attributeArguments =
            AttributeArgumentList(SeparatedList(new[] { typeOfExpression }));
        return Attribute(nameSyntax).WithArgumentList(attributeArguments);
    }

    private static AttributeSyntax CreateMessagerAttribute()
    {
        var nameSyntax = IdentifierName("Messager");
        var typeOfExpression =
            AttributeArgument(TypeOfExpression(ParseTypeName("")));
        var attributeArguments =
            AttributeArgumentList(SeparatedList(new[] { typeOfExpression }));
        return Attribute(nameSyntax).WithArgumentList(attributeArguments);
    }

    private enum AttributeToAdd
    {
        Filter,
        Watch,
        Messager
    }
}