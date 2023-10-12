using Bang.Generator.Metadata;
using System.Collections.Immutable;

namespace Bang.Generator.Templating;

public static partial class Templates
{
    public static FileTemplate ComponentTypes(string projectPrefix) => new(
        $"{projectPrefix}ComponentTypes.g.cs",
        ComponentTypesRawTypes,
        ImmutableArray.Create<TemplateSubstitution>(
            new ProjectPrefixSubstitution(),
            new ComponentIdSubstitution()
        )
    );

    private sealed class ComponentIdSubstitution : TemplateSubstitution
    {
        public ComponentIdSubstitution() : base("<component_id_list>") { }

        // The template for this uses a static getter instead of a const field.
        // This is mostly so hot reload is happy whenever we add more components to the project.
        // If this is ever a problem, we can revisit this.
        protected override string ProcessComponent(TypeMetadata.Component metadata)
        {
            var id = metadata.IsTransformComponent
                ? "global::Bang.Entities.BangComponentTypes.Transform"
                : $"global::Bang.{ParentProjectPrefix}ComponentsLookup.{ParentProjectPrefix}NextLookupId + {metadata.Index}";

            return $"""
                    
                            /// <summary>
                            /// Unique Id used for the lookup of components with type <see cref="{metadata.FullyQualifiedName}"/>.
                            /// </summary>
                            public static int {metadata.FriendlyName} => {id};

                    """;
        }
    }
}