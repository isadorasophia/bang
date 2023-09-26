﻿using Bang.Generator.Metadata;
using System.Collections.Immutable;
using System.Text;

namespace Bang.Generator.Templating;

public static partial class Templates
{
    public static FileTemplate EntityExtensions(string projectPrefix) => new(
        $"{projectPrefix}EntityExtensions.g.cs",
        EntityExtensionsRawText,
        ImmutableArray.Create<TemplateSubstitution>(
            new ProjectPrefixSubstitution(),
            new ComponentGetSubstitution(),
            new ComponentHasSubstitution(),
            new ComponentTryGetSubstitution(),
            new ComponentSetSubstitution(),
            new ComponentWithSubstitution(),
            new ComponentRemoveSubstitution(),
            new MessageHasSubstitution()
        )
    );

    private sealed class ComponentGetSubstitution : TemplateSubstitution
    {
        public ComponentGetSubstitution() : base("<components_get>") { }

        protected override string ProcessComponent(TypeMetadata.Component metadata) =>
            $"""
                     {(metadata.IsInternal ? "internal" : "public")} static global::{metadata.FullyQualifiedName} Get{metadata.FriendlyName}(this global::Bang.Entities.Entity e)
                         => e.GetComponent<global::{metadata.FullyQualifiedName}>(global::Bang.Entities.{ProjectPrefix}ComponentTypes.{metadata.FriendlyName});

             
             """;
    }

    private sealed class ComponentHasSubstitution : TemplateSubstitution
    {
        public ComponentHasSubstitution() : base("<components_has>") { }

        protected override string ProcessComponent(TypeMetadata.Component metadata) =>
            $"""
                     {(metadata.IsInternal ? "internal" : "public")} static bool Has{metadata.FriendlyName}(this global::Bang.Entities.Entity e)
                         => e.HasComponent(global::Bang.Entities.{ProjectPrefix}ComponentTypes.{metadata.FriendlyName});

             
             """;
    }

    private sealed class ComponentTryGetSubstitution : TemplateSubstitution
    {
        public ComponentTryGetSubstitution() : base("<components_try_get>") { }

        protected override string ProcessComponent(TypeMetadata.Component metadata) =>
            $"""
                     {(metadata.IsInternal ? "internal" : "public")} static global::{metadata.FullyQualifiedName}? TryGet{metadata.FriendlyName}(this global::Bang.Entities.Entity e)
                         => e.Has{metadata.FriendlyName}() ? e.Get{metadata.FriendlyName}() : null;

             
             """;
    }

    private sealed class ComponentSetSubstitution : TemplateSubstitution
    {
        public ComponentSetSubstitution() : base("<components_set>") { }

        protected override string ProcessComponent(TypeMetadata.Component metadata)
        {
            var builder = new StringBuilder();

            // Adds a special extension method for each constructor
            foreach (var constructor in metadata.Constructors)
            {
                var parameterList =
                    constructor.Parameters.Any()
                    ? $", {(string.Join(", ", constructor.Parameters.Select(parameter => $"{parameter.FullyQualifiedTypeName} {parameter.Name}")))}"
                    : "";

                var argumentList =
                    constructor.Parameters.Any()
                        ? $"{(string.Join(", ", constructor.Parameters.Select(x => x.Name)))}"
                        : "";

                builder.Append($$"""
                                         {{(metadata.IsInternal ? "internal" : "public")}} static void Set{{metadata.FriendlyName}}(this global::Bang.Entities.Entity e{{parameterList}})
                                         {
                                             e.AddOrReplaceComponent(new global::{{metadata.FullyQualifiedName}}({{argumentList}}), global::Bang.Entities.{{ProjectPrefix}}ComponentTypes.{{metadata.FriendlyName}});
                                         }
                                         

                                 """);
            }

            builder.Append($$"""
                             {{(metadata.IsInternal ? "internal" : "public")}} static void Set{{metadata.FriendlyName}}(this global::Bang.Entities.Entity e, global::{{metadata.FullyQualifiedName}} component)
                             {
                                 e.AddOrReplaceComponent(component, global::Bang.Entities.{{ProjectPrefix}}ComponentTypes.{{metadata.FriendlyName}});
                             }

                     
                     """);


            return builder.ToString();
        }
    }

    private sealed class ComponentWithSubstitution : TemplateSubstitution
    {
        public ComponentWithSubstitution() : base("<components_with>") { }

        protected override string ProcessComponent(TypeMetadata.Component metadata) =>
            $$"""
                      {{(metadata.IsInternal ? "internal" : "public")}} static global::Bang.Entities.Entity With{{metadata.FriendlyName}}(this global::Bang.Entities.Entity e, global::{{metadata.FullyQualifiedName}} component)
                      {
                          e.AddOrReplaceComponent(component, global::Bang.Entities.{{ProjectPrefix}}ComponentTypes.{{metadata.FriendlyName}});
                          return e;
                      }

              
              """;
    }

    private sealed class ComponentRemoveSubstitution : TemplateSubstitution
    {
        public ComponentRemoveSubstitution() : base("<components_remove>") { }

        protected override string ProcessComponent(TypeMetadata.Component metadata) =>
            $"""
                     {(metadata.IsInternal ? "internal" : "public")} static bool Remove{metadata.FriendlyName}(this global::Bang.Entities.Entity e)
                         => e.RemoveComponent(global::Bang.Entities.{ProjectPrefix}ComponentTypes.{metadata.FriendlyName});

             
             """;
    }

    private sealed class MessageHasSubstitution : TemplateSubstitution
    {
        public MessageHasSubstitution() : base("<messages_has>") { }

        protected override string ProcessMessage(TypeMetadata.Message metadata) =>
            $"""
                     {(metadata.IsInternal ? "internal" : "public")} static bool Has{metadata.TypeName}(this global::Bang.Entities.Entity e)
                         => e.HasComponent(global::Bang.Entities.{ProjectPrefix}MessageTypes.{metadata.FriendlyName});

             
             """;
    }
}