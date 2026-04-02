using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

[Serializable, NetSerializable]
public sealed class IFFSetMassCloakMessage : BoundUserInterfaceMessage
{
    public bool Enabled;
    public float Range;
}
