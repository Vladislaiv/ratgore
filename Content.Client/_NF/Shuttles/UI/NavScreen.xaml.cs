// NeuPanda - This file is licensed under AGPLv3
// Copyright (c) 2025 NeuPanda
// See AGPLv3.txt for details.
using System.Numerics;
using Content.Shared._NF.Shuttles.Events;
using Content.Shared.Shuttles.BUIStates;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Shuttles.UI
{
    public sealed partial class NavScreen
    {
        private readonly ButtonGroup _buttonGroup = new();
        public event Action<NetEntity?, InertiaDampeningMode>? OnInertiaDampeningModeChanged;
        public event Action<NetEntity?, Vector2>? OnSetTargetCoordinates;
        public event Action<NetEntity?, bool>? OnSetHideTarget;

        private bool _targetCoordsModified = false;

        private void NfInitialize()
        {
            var iffSearch = FindControl<LineEdit>("IffSearchCriteria");
            iffSearch.OnTextChanged += args => OnIffSearchChanged(args.Text);
            iffSearch.Text = string.Empty;
            NavRadar.IFFFilter = null;
            NavRadar.IFFLineFilter = null;

            DampenerOff.OnPressed += _ => SwitchDampenerMode(InertiaDampeningMode.Off);
            DampenerOn.OnPressed += _ => SwitchDampenerMode(InertiaDampeningMode.Dampened);
            AnchorOn.OnPressed += _ => SwitchDampenerMode(InertiaDampeningMode.Anchored);

            var group = new ButtonGroup();
            DampenerOff.Group = group;
            DampenerOn.Group = group;
            AnchorOn.Group = group;

            TargetX.OnTextChanged += _ => _targetCoordsModified = true;
            TargetY.OnTextChanged += _ => _targetCoordsModified = true;
            TargetSet.OnPressed += _ => SetTargetCoords();
            TargetShow.OnPressed += _ => SetHideTarget(!TargetShow.Pressed);
        }

        private void SwitchDampenerMode(InertiaDampeningMode mode)
        {
            NavRadar.DampeningMode = mode;
            _entManager.TryGetNetEntity(_shuttleEntity, out var shuttle);
            OnInertiaDampeningModeChanged?.Invoke(shuttle, mode);
        }

        private void OnIffSearchChanged(string text)
        {
            text = text.Trim();

            NavRadar.IFFFilter = text.Length == 0
                ? null
                : (entity, grid, iff) =>
                {
                    _entManager.TryGetComponent<MetaDataComponent>(entity, out var metadata);
                    return metadata != null && metadata.EntityName.Contains(text, StringComparison.OrdinalIgnoreCase);
                };

            NavRadar.IFFLineFilter = text.Length == 0
                ? null
                : (entity, grid, iff) =>
                {
                    _entManager.TryGetComponent<MetaDataComponent>(entity, out var metadata);
                    return metadata != null && metadata.EntityName.Contains(text, StringComparison.OrdinalIgnoreCase);
                };
        }

        private void NfUpdateState(NavInterfaceState state)
        {
            DampenerModeButtons.Visible = true;
            DampenerOff.Pressed = NavRadar.DampeningMode == InertiaDampeningMode.Off;
            DampenerOn.Pressed = NavRadar.DampeningMode == InertiaDampeningMode.Dampened;
            AnchorOn.Pressed = NavRadar.DampeningMode == InertiaDampeningMode.Anchored;

            TargetShow.Pressed = !state.HideTarget;
            if (!_targetCoordsModified)
            {
                if (state.Target != null)
                {
                    var target = state.Target.Value;
                    TargetX.Text = target.X.ToString("F1");
                    TargetY.Text = target.Y.ToString("F1");
                }
                else
                {
                    TargetX.Text = 0.0f.ToString("F1");
                    TargetY.Text = 0.0f.ToString("F1");
                }
            }
        }

        private void SetTargetCoords()
        {
            Vector2 outputVector;
            if (!float.TryParse(TargetX.Text, out outputVector.X))
                outputVector.X = 0.0f;

            if (!float.TryParse(TargetY.Text, out outputVector.Y))
                outputVector.Y = 0.0f;

            NavRadar.Target = outputVector;
            NavRadar.TargetEntity = NetEntity.Invalid;
            _entManager.TryGetNetEntity(_shuttleEntity, out var shuttle);
            OnSetTargetCoordinates?.Invoke(shuttle, outputVector);
            _targetCoordsModified = false;
        }

        private void SetHideTarget(bool hide)
        {
            _entManager.TryGetNetEntity(_shuttleEntity, out var shuttle);
            OnSetHideTarget?.Invoke(shuttle, hide);
        }
    }
}
