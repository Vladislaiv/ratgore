using Content.Server.Administration.Systems;
using Content.Server.Chat.Managers;
using Content.Server.DetailExaminable;
using Content.Server.Forensics;
using Content.Server.StationRecords.Systems;
using Content.Shared._Arcadis.Limbus;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Components;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Content.Shared.Traits.Assorted.Components;
using Content.Shared.Weapons.Melee.Events;
using Microsoft.CodeAnalysis;
using Robust.Server.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._Arcadis.Limbus;

public sealed partial class ArayashikiSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArayashikiComponent, MeleeHitEvent>(OnMeleeHit);
    }

    public override void Update(float deltaTime)
    {
        var enumerator = EntityQueryEnumerator<BeingIncineratedComponent>();
        while (enumerator.MoveNext(out var ent, out var comp))
        {
            if (comp.ErasedIntensity >= 3.5)
            {
                if (_player.TryGetSessionByEntity(ent, out var session))
                {
                    _chat.DeleteMessagesBy(session);
                }

                foreach (var item in _inventory.GetHandOrInventoryEntities(ent))
                {
                    if (TryComp(item, out PdaComponent? pda) &&
                        TryComp(pda.ContainedId, out StationRecordKeyStorageComponent? keyStorage) &&
                        keyStorage.Key is { } key &&
                        _stationRecords.TryGetRecord(key, out GeneralStationRecord? record))
                    {
                        if (TryComp(ent, out DnaComponent? dna) &&
                            dna.DNA != record.DNA)
                        {
                            continue;
                        }

                        if (TryComp(ent, out FingerprintComponent? fingerPrint) &&
                            fingerPrint.Fingerprint != record.Fingerprint)
                        {
                            continue;
                        }

                        _stationRecords.RemoveRecord(key);
                        Del(item);
                    }
                }

                QueueDel(ent);
            }
        }
    }

    public void OnMeleeHit(Entity<ArayashikiComponent> ent, ref MeleeHitEvent args)
    {
        foreach (var target in args.HitEntities)
        {
            EraseCharacterData(target, ent.Comp, true);
        }

        EraseCharacterData(args.User, ent.Comp, false);
    }

    private void EraseCharacterData(EntityUid target, ArayashikiComponent comp, bool doDeletion)
    {
        if (!TryComp<MindContainerComponent>(target, out var _))
            return;

        if (!EntityManager.TryGetComponent<MetaDataComponent>(target, out var metadata))
            return;

        var descriptionEraseAmount = Math.Ceiling(metadata.EntityDescription.Length * comp.TextErasedPercentage);
        var nameEraseAmount = Math.Ceiling(metadata.EntityName.Length * comp.TextErasedPercentage);

        // Erase specified percentage of non-space characters from name and description
        for (uint i = 0; i < descriptionEraseAmount; i++)
        {
            _meta.SetEntityDescription(target, ErasingMeErasingYou(metadata.EntityDescription));
        }
        for (uint i = 0; i < nameEraseAmount; i++)
        {
            _meta.SetEntityName(target, ErasingMeErasingYou(metadata.EntityName));
        }

        if (TryComp<DetailExaminableComponent>(target, out var detail))
        {
            var extendDescEraseAmount = Math.Ceiling(detail.Content.Length * comp.TextErasedPercentage);
            for (uint i = 0; i < extendDescEraseAmount; i++)
            {
                detail.Content = ErasingMeErasingYou(detail.Content);
            }
        }

        if (doDeletion)
        {
            var erasureComp = EnsureComp<BeingErasedComponent>(target);
            erasureComp.ErasureLevel += comp.ErasurePerHit;
            if (erasureComp.ErasureLevel >= 1)
            {
                Erased(target);
            }
        }
    }

    private void Erased(EntityUid target)
    {
        EnsureComp<BeingIncineratedComponent>(target);
        if (TryComp<InputMoverComponent>(target, out var inputMover))
        {
            inputMover.CanMove = false;
            Dirty(target, inputMover);
        }
    }

    private string ErasingMeErasingYou(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Bail early if everything is already spaces
        if (!input.Any(c => c != ' '))
            return input;

        int index;

        // keep rolling until we land on a non-space
        do
        {
            index = (_random.Next() & int.MaxValue) % input.Length;
        }
        while (input[index] == ' ');


        var chars = input.ToCharArray();
        chars[index] = ' ';
        return new string(chars);
    }
}
