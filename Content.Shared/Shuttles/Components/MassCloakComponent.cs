using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MassCloakComponent : Component
{
    // Marker component for grids currently under active mass cloaking.
}
