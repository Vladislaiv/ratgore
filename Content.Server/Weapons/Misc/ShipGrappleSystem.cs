using System.Numerics;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Misc;
using Content.Shared._Crescent.SpaceArtillery;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Weapons.Misc;

public sealed class ShipGrappleSystem : EntitySystem
{
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string GrappleJointPrefix = "ship-grapple-";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShipGrappleProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<ShipGrappleGunComponent, ComponentShutdown>(OnGunShutdown);
        SubscribeLocalEvent<ShipGrappleGunComponent, ComponentRemove>(OnGunRemove);
        SubscribeLocalEvent<ShipGrappleGunComponent, EntParentChangedMessage>(OnGunParentChanged);
        SubscribeLocalEvent<ShipGrappleGunComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ShipGrappleGunComponent, GrappleCutFinishedEvent>(OnGrappleCutFinished);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<ShipGrappleGunComponent>();
        while (query.MoveNext(out var uid, out var gun))
        {
            if (gun.JointId == null || gun.TargetGrid == null)
                continue;

            var gunGrid = Transform(uid).GridUid;
            if (gunGrid == null)
            {
                ClearGrapple(uid, gun);
                continue;
            }

            if (!TryComp<JointComponent>(gunGrid.Value, out var jointComp) ||
                !jointComp.GetJoints.TryGetValue(gun.JointId, out var joint) ||
                joint is not DistanceJoint distance)
            {
                ClearGrapple(uid, gun);
                continue;
            }

            distance.MaxLength = MathF.Max(gun.MinLength, distance.MaxLength - gun.ReelRate * frameTime);
            distance.Length = MathF.Min(distance.MaxLength, distance.Length);

            _physics.WakeBody(joint.BodyAUid);
            _physics.WakeBody(joint.BodyBUid);

            if (jointComp.Relay != null)
                _physics.WakeBody(jointComp.Relay.Value);

            Dirty(gunGrid.Value, jointComp);
        }
    }

    private void OnProjectileHit(EntityUid uid, ShipGrappleProjectileComponent component, ref ProjectileHitEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<ProjectileComponent>(uid, out var projectile) ||
            projectile.Weapon == null)
        {
            return;
        }

        var gunUid = projectile.Weapon.Value;
        if (!TryComp<ShipGrappleGunComponent>(gunUid, out var gun))
            return;

        var gunGrid = Transform(gunUid).GridUid;
        var targetGrid = Transform(args.Target).GridUid;
        if (gunGrid == null || targetGrid == null || gunGrid == targetGrid)
            return;

        if (TryComp<BlockShipWeaponProjectileGridComponent>(targetGrid.Value, out _))
            return;

        var jointComp = EnsureComp<JointComponent>(gunGrid.Value);
        RemoveExistingJoint(gunGrid.Value, gun, jointComp);

        var jointId = GrappleJointPrefix + gunUid;
        var (gunAnchor, targetAnchor, minLength) = GetGrappleAnchors(uid, args.Target, gunUid, gun, gunGrid.Value, targetGrid.Value);
        var joint = _joints.CreateDistanceJoint(gunGrid.Value, targetGrid.Value, anchorA: gunAnchor, anchorB: targetAnchor, id: jointId);
        joint.MaxLength = joint.Length + gun.Slack;
        joint.MinLength = MathF.Max(gun.MinLength, minLength);
        joint.Stiffness = gun.Stiffness;

        gun.JointId = jointId;
        gun.TargetGrid = targetGrid;
        Dirty(gunUid, gun);
        Dirty(gunGrid.Value, jointComp);

        var visuals = EnsureComp<JointVisualsComponent>(gunUid);
        visuals.Sprite = new SpriteSpecifier.Rsi(new ResPath("Objects/Weapons/Guns/Launchers/grappling_gun.rsi"), "rope");
        visuals.Target = targetGrid;
        visuals.OffsetA = Vector2.Zero;
        visuals.OffsetB = targetAnchor;
        Dirty(gunUid, visuals);

        if (_timing.IsFirstTimePredicted)
            QueueDel(uid);
    }

    private void OnGunShutdown(EntityUid uid, ShipGrappleGunComponent component, ComponentShutdown args)
    {
        ClearGrapple(uid, component);
    }

    private void OnGunRemove(EntityUid uid, ShipGrappleGunComponent component, ComponentRemove args)
    {
        ClearGrapple(uid, component);
    }

    private void OnGunParentChanged(EntityUid uid, ShipGrappleGunComponent component, ref EntParentChangedMessage args)
    {
        if (component.TargetGrid == null)
            return;

        if (args.OldParent == null)
        {
            ClearGrapple(uid, component);
            return;
        }

        if (TryComp<MapGridComponent>(args.OldParent.Value, out _))
        {
            ClearGrapple(uid, component);
        }
    }

    private void OnInteractUsing(EntityUid uid, ShipGrappleGunComponent component, InteractUsingEvent args)
    {
        if (args.Handled || component.JointId == null || component.TargetGrid == null)
            return;

        args.Handled = _toolSystem.UseTool(args.Used, args.User, uid, component.CutDelay, component.CutQuality, new GrappleCutFinishedEvent());
    }

    private void OnGrappleCutFinished(EntityUid uid, ShipGrappleGunComponent component, DoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        ClearGrapple(uid, component);
    }

    private void ClearGrapple(EntityUid uid, ShipGrappleGunComponent component)
    {
        var gridUid = Transform(uid).GridUid;
        if (gridUid != null && component.JointId != null)
        {
            if (TryComp<JointComponent>(gridUid.Value, out var jointComp))
            {
                RemoveExistingJoint(gridUid.Value, component, jointComp);
                Dirty(gridUid.Value, jointComp);
            }
        }
        else if (component.JointId != null)
        {
            component.JointId = null;
            component.TargetGrid = null;
        }

        Dirty(uid, component);

        if (HasComp<JointVisualsComponent>(uid))
            RemComp<JointVisualsComponent>(uid);
    }

    private void RemoveExistingJoint(EntityUid jointOwner, ShipGrappleGunComponent component, JointComponent jointComp)
    {
        if (component.JointId == null)
            return;

        _joints.RemoveJoint(jointOwner, component.JointId);
        component.JointId = null;
        component.TargetGrid = null;
        Dirty(jointOwner, jointComp);
    }

    private (Vector2 GunAnchor, Vector2 TargetAnchor, float MinLength) GetGrappleAnchors(
        EntityUid projectileUid,
        EntityUid targetUid,
        EntityUid gunUid,
        ShipGrappleGunComponent gun,
        EntityUid gunGridUid,
        EntityUid targetGridUid)
    {
        var gunXform = Transform(gunUid);
        var gunGridXform = Transform(gunGridUid);
        var targetGridXform = Transform(targetGridUid);

        var (gunWorldPos, gunWorldRot) = _transform.GetWorldPositionRotation(gunXform);
        var gunGridWorldPos = _transform.GetWorldPosition(gunGridXform);
        var targetGridWorldPos = _transform.GetWorldPosition(targetGridXform);

        var gunWorldDir = gunWorldRot.ToWorldVec().Normalized();
        if (gunWorldDir == Vector2.Zero)
            gunWorldDir = Vector2.UnitY;

        var impactWorldPos = _transform.GetWorldPosition(projectileUid);
        if (impactWorldPos == Vector2.Zero)
            impactWorldPos = _transform.GetWorldPosition(targetUid);
        if (impactWorldPos == Vector2.Zero)
            impactWorldPos = _transform.GetWorldPosition(targetGridXform);
        var gunAnchorWorld = gunWorldPos + gunWorldDir * Vector2.Dot(impactWorldPos - gunWorldPos, gunWorldDir);

        var gunAnchor = WorldToGridLocal(gunAnchorWorld, gunGridWorldPos, gunGridXform.LocalRotation);
        var targetAnchor = WorldToGridLocal(impactWorldPos, targetGridWorldPos, targetGridXform.LocalRotation);

        var minDistance = MathF.Max(0f, gun.MinLength + gun.GridSeparationPadding);

        return (gunAnchor, targetAnchor, minDistance);
    }

    private static Vector2 WorldToGridLocal(Vector2 worldPos, Vector2 gridWorldPos, Angle gridWorldRot)
    {
        var delta = worldPos - gridWorldPos;
        return (-gridWorldRot).RotateVec(delta);
    }
}
