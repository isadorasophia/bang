using Microsoft.CodeAnalysis;

namespace Bang.Generator.Extensions;

public static class HelperExtensions
{
    private const string Message = "Message";
    private const string Component = "Component";

    public static string ToCleanMessageName(this string value)
        => value.EndsWith(Message) ? value[..^Message.Length] : value;

    public static string ToCleanComponentName(this string value)
        => value.EndsWith(Component) ? value[..^Component.Length] : value;

    public static IEnumerable<T> Yield<T>(this T value)
    {
        yield return value;
    }

    private static readonly SymbolDisplayFormat _fullyQualifiedDisplayFormat =
        new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    public static string FullyQualifiedName(this ITypeSymbol type)
    {
        var fullyQualifiedTypeName = type.ToDisplayString(_fullyQualifiedDisplayFormat);
        // Roslyn graces us with Nullable types as `T?` instead of `Nullable<T>`, so we make an exception here.
        if (fullyQualifiedTypeName.Contains("?") || type is not INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
        {
            return fullyQualifiedTypeName;
        }

        var genericTypes = string.Join(
            ", ",
            namedTypeSymbol.TypeArguments.Select(x => $"global::{x.FullyQualifiedName()}")
        );

        return $"{fullyQualifiedTypeName}<{genericTypes}>";

    }

    /// <summary>
    /// Checks if the given <see cref="symbol"/> implements the interface <see cref="interfaceTypeSymbol"/>.
    /// </summary>
    /// <param name="type">Type declaration symbol.</param>
    /// <param name="interfaceToCheck">Interface to be checked.</param>
    /// <returns></returns>
    public static bool ImplementsInterface(
        this ITypeSymbol type,
        ISymbol? interfaceToCheck
    ) => type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceToCheck));

    /// <summary>
    /// Checks if the given <see cref="symbol"/> has an attribute defined of type <see cref="attributeToCheck"/>.
    /// </summary>
    /// <param name="type">Type declaration symbol.</param>
    /// <param name="attributeToCheck">Attribute to be checked.</param>
    /// <returns></returns>
    public static bool HasAttribute(
        this ITypeSymbol type,
        ISymbol? attributeToCheck
    ) => type.GetAttributes().Any(i => SymbolEqualityComparer.Default.Equals(i.AttributeClass, attributeToCheck));

    public static bool IsSubtypeOf(
        this ITypeSymbol type,
        ISymbol subtypeToCheck
    )
    {
        ITypeSymbol? nextTypeToVerify = type;
        do
        {
            var subtype = nextTypeToVerify?.BaseType;
            if (subtype is not null && SymbolEqualityComparer.Default.Equals(subtype, subtypeToCheck))
            {
                return true;
            }

            nextTypeToVerify = subtype;

        } while (nextTypeToVerify is not null);

        return false;
    }
}