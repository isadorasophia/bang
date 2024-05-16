using Bang.Generator.Metadata;
using System.Collections.Immutable;

namespace Bang.Generator.Templating;

public static partial class Templates
{
    public static FileTemplate WorldExtensions(string projectPrefix) => new(
        $"{projectPrefix}WorldExtensions.g.cs",
        WorldExtensionsRawText,
        ImmutableArray.Create<TemplateSubstitution>(
            new ProjectPrefixSubstitution(),
            new WorldGetUniqueSubstitution(),
            new WorldGetUniqueEntitySubstitution()
        )
    );

    private sealed class WorldGetUniqueSubstitution : TemplateSubstitution
    {
        public WorldGetUniqueSubstitution() : base("<world_getunique>") { }

        protected override string? ProcessComponent(TypeMetadata.Component metadata)
        {
            if (!metadata.IsUniqueComponent)
            {
                return null;
            }

            return
            $"""
                     /// <summary>
                     /// Tries to fetch the unique component of type <see cref="{metadata.FullyQualifiedName}" />.
                     /// </summary>
                     /// <returns>
                     /// Default value (null) on failure.
                     /// </returns>
                     {(metadata.IsInternal ? "internal" : "public")} static global::{metadata.FullyQualifiedName}? TryGetUnique{metadata.FriendlyName}(this global::Bang.World w)
                         => w.TryGetUnique<global::{metadata.FullyQualifiedName}>(global::Bang.Entities.{ProjectPrefix}ComponentTypes.{metadata.FriendlyName});

                     /// <summary>
                     /// Fetches the unique component of type <see cref="{metadata.FullyQualifiedName}" />.
                     /// </summary>
                     {(metadata.IsInternal ? "internal" : "public")} static global::{metadata.FullyQualifiedName} GetUnique{metadata.FriendlyName}(this global::Bang.World w)
                         => w.GetUnique<global::{metadata.FullyQualifiedName}>(global::Bang.Entities.{ProjectPrefix}ComponentTypes.{metadata.FriendlyName});             


             """;
        }
    }

    private sealed class WorldGetUniqueEntitySubstitution : TemplateSubstitution
    {
        public WorldGetUniqueEntitySubstitution() : base("<world_getuniqueentity>") { }

        protected override string? ProcessComponent(TypeMetadata.Component metadata)
        {
            if (!metadata.IsUniqueComponent)
            {
                return null;
            }

            return
            $"""
                     /// <summary>
                     /// Tries to fetch the entity with an unique component of type <see cref="{metadata.FullyQualifiedName}" />.
                     /// </summary>
                     /// <returns>
                     /// Default value (null) on failure.
                     /// </returns>
                     {(metadata.IsInternal ? "internal" : "public")} static global::Bang.Entities.Entity? TryGetUniqueEntity{metadata.FriendlyName}(this global::Bang.World w)
                         => w.TryGetUniqueEntity<global::{metadata.FullyQualifiedName}>(global::Bang.Entities.{ProjectPrefix}ComponentTypes.{metadata.FriendlyName});

                     /// <summary>
                     /// Fetches the entity with an unique component of type <see cref="{metadata.FullyQualifiedName}" />.
                     /// </summary>
                     {(metadata.IsInternal ? "internal" : "public")} static global::Bang.Entities.Entity GetUniqueEntity{metadata.FriendlyName}(this global::Bang.World w)
                         => w.GetUniqueEntity<global::{metadata.FullyQualifiedName}>(global::Bang.Entities.{ProjectPrefix}ComponentTypes.{metadata.FriendlyName});


             """;
        }
    }
}