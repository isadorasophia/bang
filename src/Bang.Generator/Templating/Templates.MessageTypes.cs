using Bang.Generator.Metadata;
using System.Collections.Immutable;

namespace Bang.Generator.Templating;

public static partial class Templates
{
    public static FileTemplate MessageTypes(string projectPrefix) => new(
        $"{projectPrefix}MessageTypes.g.cs",
        MessageTypesRawText,
        ImmutableArray.Create<TemplateSubstitution>(
            new ProjectPrefixSubstitution(),
            new MessageIdSubstitution()
        )
    );

    private sealed class MessageIdSubstitution : TemplateSubstitution
    {
        public MessageIdSubstitution() : base("<message_id_list>") { }

        // The template for this uses a static getter instead of a const field.
        // This is mostly so hot reload is happy whenever we add more components to the project.
        // If this is ever a problem, we can revisit this.
        protected override string ProcessMessage(TypeMetadata.Message metadata)
            => $"""
                
                        /// <summary>
                        /// Unique Id used for the lookup of messages with type <see cref="{metadata.FullyQualifiedName}"/>.
                        /// </summary>
                        public static int {metadata.FriendlyName} => global::Bang.{ParentProjectPrefix}ComponentsLookup.{ParentProjectPrefix}NextLookupId + {metadata.Index};
                
                """;
    }
}