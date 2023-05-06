namespace Bang
{
    /// <summary>
    /// Attribute used on private readonly fields that should be persisted.
    /// This is to workaround System.Text.Json limitation that JsonProperty is only
    /// allowed on public properties. 
    /// If this is a readonly field, you need to make sure there is a compatible 
    /// [JsonConstructor] declared.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class PersistAttribute : Attribute
    {
    }
}
