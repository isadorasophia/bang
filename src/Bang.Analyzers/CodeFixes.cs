namespace Bang.Analyzers;

public static class CodeFixes
{
    public static class ReadonlyStruct
    {
        public const string Title = "Convert to readonly struct";
    }

    public static class RemoveAttribute
    {
        public const string Title = "Remove attribute";
    }

    public static class AddAttribute
    {
        public static string Title(string attributeName) => $"Add {attributeName} attribute";
    }

    public static class AddInterface
    {
        public static string Title(string interfaceName) => $"Add {interfaceName} interface";
    }
}