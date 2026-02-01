using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Misc;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShipGrappleGunComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("reelRate"), AutoNetworkedField]
    public float ReelRate = 8f;

    [ViewVariables(VVAccess.ReadWrite), DataField("minLength"), AutoNetworkedField]
    public float MinLength = 5f;

    [ViewVariables(VVAccess.ReadWrite), DataField("slack"), AutoNetworkedField]
    public float Slack = 2f;

    [ViewVariables(VVAccess.ReadWrite), DataField("stiffness"), AutoNetworkedField]
    public float Stiffness = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField("gridSeparationPadding"), AutoNetworkedField]
    public float GridSeparationPadding = 0.5f;

    [ViewVariables(VVAccess.ReadWrite), DataField("cutDelay"), AutoNetworkedField]
    public float CutDelay = 1.0f;

    [ViewVariables(VVAccess.ReadWrite), DataField("cutQuality"), AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> CutQuality = "Slicing";

    [ViewVariables]
    public string? JointId;

    [ViewVariables]
    public EntityUid? TargetGrid;
}
