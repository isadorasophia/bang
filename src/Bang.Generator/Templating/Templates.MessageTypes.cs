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

        protected override string ProcessMessage(TypeMetadata.Message metadata)
            => $"""
                        public const int {metadata.FriendlyName} = global::Bang.{ParentProjectPrefix}ComponentsLookup.NextLookupId + {metadata.Index};

                """;
    }
}
