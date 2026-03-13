using Robust.Shared.GameStates;

namespace Content.Shared._Rat.Squad;

/// <summary>
/// Компонент для принадлежности сущности к отряду.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SquadComponent : Component
{
    /// <summary>
    /// ID отряда
    /// </summary>
    [DataField, AutoNetworkedField]
    public int SquadId;

    /// <summary>
    /// Название отряда для отображения
    /// </summary>
    [DataField, AutoNetworkedField]
    public string SquadName = string.Empty;
}
