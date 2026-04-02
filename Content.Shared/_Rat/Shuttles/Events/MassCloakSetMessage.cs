using Robust.Shared.Serialization;

namespace Content.Shared._Rat.Shuttles.Events;

[Serializable, NetSerializable]
public sealed class MassCloakSetMessage : BoundUserInterfaceMessage
{
    public bool Enabled;
    public float Range;
}
