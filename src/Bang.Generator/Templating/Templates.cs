﻿namespace Bang.Generator.Templating;

public static partial class Templates
{
    public const string ComponentTypesRawTypes =
        """
        namespace Bang.Entities
        {
            /// <summary>
            /// Collection of all ids for fetching components declared in this project.
            /// </summary>
            public static class <project_prefix>ComponentTypes
            {<component_id_list>    }
        }
        """;

    public const string MessageTypesRawText =
        """
        namespace Bang.Entities
        {
            /// <summary>
            /// Collection of all ids for fetching components declared in this project.
            /// </summary>
            public static class <project_prefix>MessageTypes
            {<message_id_list>    }
        }
        """;

    public const string EntityExtensionsRawText =
        """
        namespace Bang.Entities
        {
            /// <summary>
            /// Quality of life extensions for the components declared in this project.
            /// </summary>
            public static class <project_prefix>EntityExtensions
            {
                #region Component "Get" methods!

        <components_get>        #endregion
                
                #region Component "Has" checkers!

        <components_has>        #endregion
                
                #region Component "TryGet" methods!

        <components_try_get>        #endregion
                
                #region Component "Set" methods!

        <components_set>        #endregion
                
                #region Component "With" methods!

        <components_with>        #endregion
                
                #region Component "Remove" methods!

        <components_remove>        #endregion
        
                #region Message "Has" checkers!

        <messages_has>        #endregion
            }
        }
        """;

    public const string LookupImplementationRaw =
        """
        using System.Collections.Immutable;
        using System.Linq;

        namespace Bang
        {
            /// <summary>
            /// Auto-generated implementation of <see cref="Bang.ComponentsLookup" /> for this project.
            /// </summary>
            public class <project_prefix>ComponentsLookup : <parent_project_lookup>
            {
                /// <summary>
                /// First lookup id a <see cref="Bang.ComponentsLookup"/> implementation that inherits from this class must use.
                /// </summary>
                <component_count_const>
                /// <summary>
                /// Default constructor. This is only relevant for the internals of Bang, so you can ignore it.
                /// </summary>
                public <project_prefix>ComponentsLookup()
                {
                    MessagesIndex = base.MessagesIndex.Concat(_messagesIndex).ToImmutableDictionary();
                    ComponentsIndex = base.ComponentsIndex.Concat(_componentsIndex).ToImmutableDictionary();
                    RelativeComponents = base.RelativeComponents.Concat(_relativeComponents).ToImmutableHashSet();
                }
        
                private static readonly ImmutableHashSet<int> _relativeComponents = new HashSet<int>()
                {
        <relative_components_set>        }.ToImmutableHashSet();
        
                private static readonly ImmutableDictionary<Type, int> _componentsIndex = new Dictionary<Type, int>()
                {
        <components_type_to_index>        }.ToImmutableDictionary();
        
                private static readonly ImmutableDictionary<Type, int> _messagesIndex = new Dictionary<Type, int>()
                {
        <messages_type_to_index>        }.ToImmutableDictionary();
            }
        }
        """;
}
