namespace Bang;

/// <summary>
/// Indicates that the property or field should be included for serialization and deserialization.
/// </summary>
/// <remarks>
/// When applied to a public property, indicates that non-public getters and setters should be used for serialization and deserialization.
/// This supports non-public properties or fields, which is why we use this instead of <see cref="System.Text.Json.Serialization.JsonIncludeAttribute"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class SerializeAttribute : Attribute
{
    public SerializeAttribute() { }
}