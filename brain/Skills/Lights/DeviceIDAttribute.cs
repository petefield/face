namespace Brain.Skills;

/// <summary>The Home Assistant entity ID (without the domain prefix) for a device.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class DeviceIDAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}
