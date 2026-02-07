// NeuPanda - This file is licensed under AGPLv3
// Copyright (c) 2025 NeuPanda
// See AGPLv3.txt for details.
using System.Numerics;
using Content.Client.Shuttles.UI;
using Content.Shared._NF.Shuttles.Events;

namespace Content.Client.Shuttles.BUI
{
    public sealed partial class ShuttleConsoleBoundUserInterface
    {
        private void NfOpen()
        {
            _window ??= new ShuttleConsoleWindow();
            _window.OnInertiaDampeningModeChanged += OnInertiaDampeningModeChanged;
            _window.OnSetTargetCoordinates += OnSetTargetCoordinates;
            _window.OnSetHideTarget += OnSetHideTarget;
        }
        private void OnInertiaDampeningModeChanged(NetEntity? entityUid, InertiaDampeningMode mode)
        {
            SendMessage(new SetInertiaDampeningRequest
            {
                ShuttleEntityUid = entityUid,
                Mode = mode,
            });
        }

        private void OnSetTargetCoordinates(NetEntity? entityUid, Vector2 position)
        {
            SendMessage(new SetTargetCoordinatesRequest
            {
                ShuttleEntityUid = entityUid,
                TrackedPosition = position,
                TrackedEntity = NetEntity.Invalid
            });
        }

        private void OnSetHideTarget(NetEntity? entityUid, bool hide)
        {
            SendMessage(new SetHideTargetRequest
            {
                Hidden = hide
            });
        }

    }
}
