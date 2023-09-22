using Bang.Generator.Metadata;
using System.Collections.Immutable;

namespace Bang.Generator.Templating;

public static partial class Templates
{
    public static FileTemplate LookupImplementation(string projectPrefix) => new(
        $"{projectPrefix}ComponentsLookup.g.cs",
        LookupImplementationRaw,
        ImmutableArray.Create<TemplateSubstitution>(
            new ParentProjectLookupClassSubstitution(),
            new ProjectPrefixSubstitution(),
            new RelativeComponentSetSubstitution(),
            new ComponentTypeToIndexMapSubstitution(),
            new MessageTypeToIndexMapSubstitution(),
            new IdCountSubstitution()
        )
    );

    private sealed class ParentProjectLookupClassSubstitution : TemplateSubstitution
    {
        public ParentProjectLookupClassSubstitution() : base("<parent_project_lookup>") { }

        protected override string ProcessProject(TypeMetadata.Project project)
            => $"global::{project.ParentProjectLookupClassName}";
    }

    private sealed class RelativeComponentSetSubstitution : TemplateSubstitution
    {
        public RelativeComponentSetSubstitution() : base("<relative_components_set>") { }

        protected override string? ProcessComponent(TypeMetadata.Component metadata)
            => metadata.IsParentRelativeComponent
            ? $"""
                           global::Bang.Entities.{ProjectPrefix}ComponentTypes.{metadata.FriendlyName},

               """
            : null;
    }

    private sealed class ComponentTypeToIndexMapSubstitution : TemplateSubstitution
    {
        public ComponentTypeToIndexMapSubstitution() : base("<components_type_to_index>") { }

        protected override string ProcessComponent(TypeMetadata.Component metadata) =>
            $$"""
                          { typeof(global::{{metadata.FullyQualifiedName}}), global::Bang.Entities.{{ProjectPrefix}}ComponentTypes.{{metadata.FriendlyName}} },

              """;

        protected override string ProcessStateMachine(TypeMetadata.StateMachine metadata) =>
            $$"""
                          { typeof(global::Bang.StateMachines.StateMachineComponent<global::{{metadata.FullyQualifiedName}}>), global::Bang.Entities.BangComponentTypes.StateMachine },

              """;


        protected override string ProcessInteraction(TypeMetadata.Interaction metadata) =>
            $$"""
                          { typeof(global::Bang.Interactions.InteractiveComponent<global::{{metadata.FullyQualifiedName}}>), global::Bang.Entities.BangComponentTypes.Interactive },

              """;
    }

    private sealed class MessageTypeToIndexMapSubstitution : TemplateSubstitution
    {
        public MessageTypeToIndexMapSubstitution() : base("<messages_type_to_index>") { }

        protected override string ProcessMessage(TypeMetadata.Message metadata) =>
            $$"""
                          { typeof(global::{{metadata.FullyQualifiedName}}), global::Bang.Entities.{{ProjectPrefix}}MessageTypes.{{metadata.FriendlyName}} },

              """;
    }

    private sealed class IdCountSubstitution : TemplateSubstitution
    {
        private int idCount;
        public IdCountSubstitution() : base("<component_count_const>") { }

        protected override string? ProcessComponent(TypeMetadata.Component metadata)
        {
            idCount++;
            return base.ProcessComponent(metadata);
        }

        protected override string? ProcessMessage(TypeMetadata.Message metadata)
        {
            idCount++;
            return base.ProcessMessage(metadata);
        }

        protected override string FinalModification()
            => $"""
                public const int {ProjectPrefix}NextLookupId = {idCount} + {ParentProjectPrefix}ComponentsLookup.{ParentProjectPrefix}NextLookupId;

                """;
    }
}
