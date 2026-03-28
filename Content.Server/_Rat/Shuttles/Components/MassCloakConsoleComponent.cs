using Content.Server._Rat.Shuttles.Systems;

namespace Content.Server._Rat.Shuttles.Components;

[RegisterComponent, Access(typeof(MassCloakConsoleSystem))]
public sealed partial class MassCloakConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("massCloakEnabled")]
    public bool MassCloakEnabled = false;

    [ViewVariables(VVAccess.ReadWrite), DataField("massCloakRange")]
    public float MassCloakRange = 20f;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? originalGrid = null;

    public const float MassCloakMinRange = 20f;
    public const float MassCloakMaxRange = 500f;
}