using Bang.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Bang.Analyzers.CodeFixProviders;

public abstract class AddBaseTypeCodeFix : CodeFixProvider
{
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

            var typeDeclarationSyntax = FindTypeDeclarationSyntaxFromDiagnosticSpan(root, diagnosticSpan);
            if (typeDeclarationSyntax is null)
                return;
            
            var semanticModel = await context.Document.GetSemanticModelAsync();
            var typeSymbol = semanticModel?.GetDeclaredSymbol(typeDeclarationSyntax);
            if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                return;

            var baseTypeSuggestions = GetBaseTypeSuggestions(typeDeclarationSyntax, namedTypeSymbol);
            
            foreach (BaseTypeSuggestion baseTypeSuggestion in baseTypeSuggestions )
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixes.AddInterface.Title(NameOf(baseTypeSuggestion)),
                        createChangedDocument: c => AddBase(context.Document, baseTypeSuggestion, typeDeclarationSyntax, c),
                        equivalenceKey: nameof(CodeFixes.AddInterface)),
                    diagnostic
                );   
            }
        }
    }

    protected abstract IEnumerable<BaseTypeSuggestion> GetBaseTypeSuggestions(
        TypeDeclarationSyntax typeDeclarationSyntax,
        INamedTypeSymbol namedTypeSymbol
    );

    protected abstract TypeDeclarationSyntax? FindTypeDeclarationSyntaxFromDiagnosticSpan(
        SyntaxNode root, TextSpan diagnosticSpan
    );

    private async Task<Document> AddBase(
        Document document,
        BaseTypeSuggestion baseTypeSuggestion,
        TypeDeclarationSyntax typeDeclarationSyntax,
        CancellationToken cancellationToken)
    {
        var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (oldRoot is null)
            return document;

        var typeSyntax = ParseTypeName(NameOf(baseTypeSuggestion));
        var baseTypeSyntax = SimpleBaseType(typeSyntax);
        var newBaseListSyntax =
            typeDeclarationSyntax.BaseList?.AddTypes(baseTypeSyntax) ??
            BaseList(SeparatedList<BaseTypeSyntax>(new[] { baseTypeSyntax }));
        var newTypeDeclarationSyntax = typeDeclarationSyntax.WithBaseList(newBaseListSyntax);
        var rootWithAttribute = oldRoot.ReplaceNode(typeDeclarationSyntax, newTypeDeclarationSyntax);

        var namespaceForSuggestion = NamespaceFor(baseTypeSuggestion);
        var namespaceIsImported = rootWithAttribute
            .ChildNodes()
            .OfType<UsingDirectiveSyntax>()
            .Any(u => u.Name.ToString() == namespaceForSuggestion);

        if (namespaceIsImported)
            return document.WithSyntaxRoot(rootWithAttribute);

        if (rootWithAttribute is not CompilationUnitSyntax compilationUnit)
            return document.WithSyntaxRoot(rootWithAttribute);

        var namespaceParts = namespaceForSuggestion.Split('.');
        var firstToken = IdentifierName(namespaceParts.First());
        var secondToken = IdentifierName(namespaceParts.Last());
        var name = QualifiedName(firstToken, secondToken);
        var rootWithImportedNamespace = compilationUnit.AddUsings(UsingDirective(name));

        return document.WithSyntaxRoot(rootWithImportedNamespace);
    }

    public enum BaseTypeSuggestion
    {
        IComponent,
        IInteraction,
        StateMachine
    }
    
    private static string NamespaceFor(BaseTypeSuggestion baseTypeSuggestion) => baseTypeSuggestion switch
    {
        BaseTypeSuggestion.IComponent => "Bang.Components",
        BaseTypeSuggestion.IInteraction => "Bang.Interactions",
        BaseTypeSuggestion.StateMachine => "Bang.StateMachines",
        _ => throw new ArgumentOutOfRangeException(nameof(baseTypeSuggestion), baseTypeSuggestion, null)
    };

    private static string NameOf(BaseTypeSuggestion baseTypeSuggestion) => baseTypeSuggestion switch
    {
        BaseTypeSuggestion.IComponent => "IComponent",
        BaseTypeSuggestion.IInteraction => "IInteraction",
        BaseTypeSuggestion.StateMachine => "StateMachine",
        _ => throw new ArgumentOutOfRangeException(nameof(baseTypeSuggestion), baseTypeSuggestion, null)
    };
}