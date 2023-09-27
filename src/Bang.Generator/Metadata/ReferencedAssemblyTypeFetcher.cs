using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Bang.Generator.Metadata;

public sealed class ReferencedAssemblyTypeFetcher
{
    private readonly Compilation compilation;
    private ImmutableArray<INamedTypeSymbol>? cacheOfAllTypesInReferencedAssemblies;

    public ReferencedAssemblyTypeFetcher(Compilation compilation)
    {
        this.compilation = compilation;
    }

    private ImmutableArray<INamedTypeSymbol> AllTypesInReferencedAssemblies()
    {
        if (cacheOfAllTypesInReferencedAssemblies is not null)
            return cacheOfAllTypesInReferencedAssemblies.Value;

        var allTypesInReferencedAssemblies =
               compilation.SourceModule.ReferencedAssemblySymbols
                   .SelectMany(assemblySymbol =>
                       assemblySymbol
                           .GlobalNamespace.GetNamespaceMembers()
                           .SelectMany(GetAllTypesInNamespace))
                           .ToImmutableArray();

        cacheOfAllTypesInReferencedAssemblies = allTypesInReferencedAssemblies;
        return allTypesInReferencedAssemblies;
    }

    public ImmutableArray<INamedTypeSymbol> GetAllCompiledClassesWithSubtypes()
        => AllTypesInReferencedAssemblies()
            .Where(typeSymbol => !typeSymbol.IsValueType && typeSymbol.BaseType is not null)
            .ToImmutableArray();

    // Recursive method to get all types in a namespace, including nested types.
    private static IEnumerable<INamedTypeSymbol> GetAllTypesInNamespace(INamespaceSymbol namespaceSymbol)
    {
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            yield return type;
        }

        var nestedTypes =
            from nestedNamespace in namespaceSymbol.GetNamespaceMembers()
            from nestedType in GetAllTypesInNamespace(nestedNamespace)
            select nestedType;

        foreach (var nestedType in nestedTypes)
        {
            yield return nestedType;
        }
    }
}
