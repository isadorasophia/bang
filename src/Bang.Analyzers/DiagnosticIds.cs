namespace Bang.Analyzers;

public static class Diagnostics
{
    public static class Components
    {
        public static class ClassesCannotBeComponents
        {
            public const string Id = "BANG0001";
            public const string Message = "Classes cannot be components";
        };

        public static class ComponentsMustBeReadonly
        {
            public const string Id = "BANG0002";
            public const string Message = "Components must be declared as readonly";
        }

        public class ComponentsCannotBeMessages
        {
            public const string Id = "BANG0003";
            public const string Message = "Components cannot also be messages";
        }

        public class ComponentsCannotBeInteractions
        {
            public const string Id = "BANG0004";
            public const string Message = "Components cannot also be interactions";
        }
    }

    public static class Systems
    {
        public static class FilterAttribute
        {
            public const string Id = "BANG1001";
            public const string Message = "System requires FilterAttribute";
        }

        public static class MessagerAttribute
        {
            public const string Id = "BANG1002";
            public const string Message = "System requires MessagerAttribute";
        }

        public static class WatchAttribute
        {
            public const string Id = "BANG1003";
            public const string Message = "System requires WatchAttribute";
        }

        public static class NonApplicableMessagerAttribute
        {
            public const string Id = "BANG1004";
            public const string Message = "System does not use MessagerAttribute";
        }

        public static class NonApplicableWatchAttribute
        {
            public const string Id = "BANG1005";
            public const string Message = "System does not use WatchAttribute";
        }
    }

    public static class Attributes
    {
        public static class NonComponentsOnFilterAttribute
        {
            public const string Id = "BANG2001";
            public const string Message = "Filter attribute expects only components";
        }

        public static class NonMessagesOnMessagerAttribute
        {
            public const string Id = "BANG2002";
            public const string Message = "Messager attribute expects only messages";
        }

        public static class NonComponentsOnWatchAttribute
        {
            public const string Id = "BANG2003";
            public const string Message = "Watch attribute expects only components";
        }

        public static class NonApplicableUniqueAttribute
        {
            public const string Id = "BANG2004";
            public const string Message = "Unique attribute must be used only on components, state machines and interactions";
        }
    }

    public static class Messages
    {
        public static class ClassesCannotBeMessages
        {
            public const string Id = "BANG3001";
            public const string Message = "Classes cannot be messages";
        }

        public static class MessagesMustBeReadonly
        {
            public const string Id = "BANG3002";
            public const string Message = "Messages must be declared as readonly";
        }

        public class MessagesCannotBeInteractions
        {
            public const string Id = "BANG3003";
            public const string Message = "Messages cannot also be interactions";
        }
    }

    public static class World
    {
        public static class MarkUniqueComponentAsUnique
        {
            public const string Id = "BANG4001";
            public const string Message = "Trying to get an unique component not marked with the Unique attribute";
        }
    }

    public static class Interactions
    {
        public static class ClassesCannotBeInteractions
        {
            public const string Id = "BANG5001";
            public const string Message = "Classes cannot be interactions";
        }

        public static class InteractionsMustBeReadonly
        {
            public const string Id = "BANG5002";
            public const string Message = "Interactions must be declared as readonly";
        }
    }
}