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

        protected override string ProcessComponent(TypeMetadata.Component metadata)
            => $"""
                
                        /// <summary>
                        /// Unique Id used for the lookup of components with type <see cref="{metadata.FullyQualifiedName}"/>.
                        /// </summary>
                        public const int {metadata.FriendlyName} = global::Bang.{ParentProjectPrefix}ComponentsLookup.{ParentProjectPrefix}NextLookupId + {metadata.Index};

                """;
    }
}
