using Microsoft.CodeAnalysis;

namespace Bang.Analyzers.Extensions;

/// <summary>
/// Helpers for better readability when dealing with <see cref="ITypeSymbol"/>.
/// </summary>
internal static class TypeSymbolExtensions
{
    /// <summary>
    /// Checks if the given <see cref="symbol"/> implements the interface <see cref="interfaceTypeSymbol"/>.
    /// </summary>
    /// <param name="symbol">Type declaration symbol.</param>
    /// <param name="interfaceTypeSymbol">Interface to be checked.</param>
    /// <returns></returns>
    internal static bool ImplementsInterface(
        this ITypeSymbol symbol,
        ISymbol interfaceTypeSymbol
    ) => symbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceTypeSymbol));


    internal static bool HasAttribute(this INamedTypeSymbol type, ISymbol? attributeToCheck)
        => type.GetAttributes().Any(attr =>
            attr.AttributeClass is not null && attr.AttributeClass.Equals(attributeToCheck,
                SymbolEqualityComparer.IncludeNullability));

    internal static bool RecursivelyCheckForAttribute(this INamedTypeSymbol type, ISymbol? attributeToCheck)
    {
        bool hasAttribute;
        var typeToCheck = type;
        do
        {
            hasAttribute = typeToCheck.HasAttribute(attributeToCheck);

            if (!hasAttribute)
            {
                typeToCheck = typeToCheck.BaseType;
            }

        } while (!hasAttribute && typeToCheck != null);

        return hasAttribute;
    }
}