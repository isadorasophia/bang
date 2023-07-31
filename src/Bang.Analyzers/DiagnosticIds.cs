namespace Bang.Analyzers;

public static class Diagnostics
{
    public static class Components
    {
        public static class ClassesCannotBeComponents
        {
            public const string Id = "BANG0001";
            public const string Message = "Classes cannot be components.";
        };

        public static class ComponentsMustBeReadonly
        {
            public const string Id = "BANG0002";
            public const string Message = "Components must be declared as readonly.";
        }
    }

    public static class Systems
    {
        public static class MessagerAttribute
        {
            public const string Id = "BANG1001";
            public const string Message = "System requires MessagerAttribute.";
        }

        public static class WatchAttribute
        {
            public const string Id = "BANG1002";
            public const string Message = "System requires WatchAttribute.";
        }
    }

    public static class Messages
    {
        public static class ClassesCannotBeMessages
        {
            public const string Id = "BANG4001";
            public const string Message = "Classes cannot be messages.";
        };

        public static class MessagesMustBeReadonly
        {
            public const string Id = "BANG4002";
            public const string Message = "Messages must be declared as readonly.";
        }
    }
}
