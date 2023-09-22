using Bang.Generator.Extensions;
using Bang.Generator.Metadata;
using System.Collections.Immutable;
using Bang.Generator.Templating;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Bang.Generator;

[Generator]
public sealed class BangExtensionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var potentialComponents = context.PotentialComponents().Collect();
        var stateMachines = context.PotentialStateMachines().Collect();
        var compilation = potentialComponents
            .Combine(stateMachines)
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(
            compilation,
            (c, t) => Execute(c, t.Right, t.Left.Left, t.Left.Right)
        );
    }

    public void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<TypeDeclarationSyntax> potentialComponents,
        ImmutableArray<ClassDeclarationSyntax> potentialStateMachines)
    {
#if DEBUG
        // Uncomment this if you need to use a debugger.
        // if (!System.Diagnostics.Debugger.IsAttached)
        // {
        //     System.Diagnostics.Debugger.Launch();
        // }
#endif

        // Bail if any important type symbol is not resolvable.
        var bangTypeSymbols = BangTypeSymbols.FromCompilation(compilation);
        if (bangTypeSymbols is null)
            return;

        var referencedAssemblyTypeFetcher = new ReferencedAssemblyTypeFetcher(compilation);

        // Gets the best possible name for the parent lookup class assemblies.
        var parentLookupClass = referencedAssemblyTypeFetcher
            .GetAllCompiledClassesWithSubtypes()
            .Where(t => t.IsSubtypeOf(bangTypeSymbols.ComponentsLookupClass))
            .OrderBy(NumberOfParentClasses)
            .FirstOrDefault() ?? bangTypeSymbols.ComponentsLookupClass;

        var projectName = compilation.AssemblyName?.Replace(".", "") ?? "My";

        var metadataFetcher = new MetadataFetcher(compilation);

        // All files that will be created by this generator.
        var templates = ImmutableArray.Create(
            Templates.ComponentTypes(projectName),
            Templates.MessageTypes(projectName),
            Templates.EntityExtensions(projectName),
            Templates.LookupImplementation(projectName)
        );

        var projectMetadata = new TypeMetadata.Project(
            projectName,
            parentLookupClass.Name.Replace("ComponentsLookup", ""),
            parentLookupClass.FullyQualifiedName()
        );
        foreach (var template in templates)
        {
            template.Process(projectMetadata);
        }

        // Fetch all relevant metadata.
        var allTypeMetadata =
            metadataFetcher.FetchMetadata(
                bangTypeSymbols,
                potentialComponents,
                potentialStateMachines
            );

        // Process metadata.
        foreach (var metadata in allTypeMetadata)
        {
            foreach (var template in templates)
            {
                template.Process(metadata);
            }
        }

        // Generate sources.
        foreach (var template in templates)
        {
            context.AddSource(template.FileName, template.GetDocumentWithReplacements());
        }
    }

    private static int NumberOfParentClasses(INamedTypeSymbol type)
        => type.BaseType is null ? 0 : 1 + NumberOfParentClasses(type.BaseType);
}
