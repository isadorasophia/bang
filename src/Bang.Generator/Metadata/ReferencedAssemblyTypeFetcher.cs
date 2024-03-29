﻿using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Bang.Generator.Metadata;

public sealed class ReferencedAssemblyTypeFetcher
{
    private readonly Compilation _compilation;
    private ImmutableArray<INamedTypeSymbol>? _cacheOfAllTypesInReferencedAssemblies;

    public ReferencedAssemblyTypeFetcher(Compilation compilation)
    {
        this._compilation = compilation;
    }

    private ImmutableArray<INamedTypeSymbol> AllTypesInReferencedAssemblies()
    {
        if (_cacheOfAllTypesInReferencedAssemblies is not null)
            return _cacheOfAllTypesInReferencedAssemblies.Value;

        var allTypesInReferencedAssemblies =
               _compilation.SourceModule.ReferencedAssemblySymbols
                   .SelectMany(assemblySymbol =>
                       assemblySymbol
                           .GlobalNamespace.GetNamespaceMembers()
                           .SelectMany(GetAllTypesInNamespace))
                           .ToImmutableArray();

        _cacheOfAllTypesInReferencedAssemblies = allTypesInReferencedAssemblies;
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