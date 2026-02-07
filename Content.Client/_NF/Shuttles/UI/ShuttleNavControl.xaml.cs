// NeuPanda - This file is licensed under AGPLv3
// Copyright (c) 2025 NeuPanda
// See AGPLv3.txt for details.
using System.Numerics;
using Content.Shared._NF.Shuttles.Events;
using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Physics.Components;

namespace Content.Client.Shuttles.UI
{
    public sealed partial class ShuttleNavControl
    {
        public InertiaDampeningMode DampeningMode { get; set; }
        private static readonly Color TargetColor = Color.FromHex("#99ff66");
        public bool HideTarget { get; set; } = false;
        public Vector2? Target { get; set; } = null;
        public NetEntity? TargetEntity { get; set; } = null;

        private void NfUpdateState(NavInterfaceState state)
        {

            if (!EntManager.GetCoordinates(state.Coordinates).HasValue ||
                !EntManager.TryGetComponent(EntManager.GetCoordinates(state.Coordinates).GetValueOrDefault().EntityId, out TransformComponent? transform) ||
                !EntManager.TryGetComponent(transform.GridUid, out PhysicsComponent? physicsComponent))
            {
                return;
            }

            DampeningMode = state.DampeningMode;
            Target = state.Target;
            TargetEntity = state.TargetEntity;
            HideTarget = state.HideTarget;
        }
    }
}
