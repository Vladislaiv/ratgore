using Content.Shared.Crescent.Dispenser;  
using Content.Shared.Interaction;  
using Content.Shared.Inventory.VirtualItem;  
using Content.Shared.Popups;  
using Robust.Shared.Audio.Systems;  
using Robust.Shared.Prototypes;  
  
namespace Content.Server.Crescent.Dispenser;  
  
public sealed class DispenserSystem : SharedDispenserSystem  
{  
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;  
    [Dependency] private readonly SharedVirtualItemSystem _virtualItemSystem = default!;  
    [Dependency] private readonly StationTradeMarketSystem _marketSystem = default!;  
    [Dependency] private readonly Stack.StackSystem _stackSystem = default!;  
    [Dependency] private readonly SharedPopupSystem _popup = default!;  
  
    public override void Initialize()  
    {  
        base.Initialize();  
        SubscribeLocalEvent<DispenserComponent, ActivateInWorldEvent>(OnActivateInWorld);  
        SubscribeLocalEvent<DispenserComponent, InteractUsingEvent>(OnInteractUsing);  
    }  
  
    private void OnActivateInWorld(EntityUid uid, DispenserComponent component, ActivateInWorldEvent args)  
    {  
        if (args.Handled || component.Dispensing)  
            return;  
  
        if (!string.IsNullOrEmpty(component.DefaultItem))  
        {  
            args.Handled = true;  
            TryDispenseItem(uid, component, component.DefaultItem);  
        }  
        else  
        {  
            _audioSystem.PlayPvs(component.DenySound, uid);  
        }  
    }  
  
    private void OnInteractUsing(EntityUid uid, DispenserComponent component, InteractUsingEvent args)  
    {  
        if (args.Handled || component.Dispensing)  
            return;  
  
        EntityUid used;  
        if (TryComp<VirtualItemComponent>(args.Used, out var virtualItem))  
            used = virtualItem.BlockingEntity;  
        else  
            used = args.Used;  
  
        if (!TryPrototype(used, out var prototype))  
        {  
            _audioSystem.PlayPvs(component.DenySound, uid);  
            return;  
        }  

        if (component.DynamicInventory.TryGetValue(prototype.ID, out var baseAmount))  
        {  
            args.Handled = true;  
 
            var stationUid = _marketSystem.TryGetOwningStation(uid);  

            float multiplier = stationUid.HasValue  
                ? _marketSystem.GetPriceMultiplier(stationUid.Value, prototype.ID)  
                : 1.0f;  
  
            int finalAmount = (int)MathF.Round(baseAmount * multiplier);  

            if (stationUid.HasValue)  
                _marketSystem.RecordSale(stationUid.Value, prototype.ID);  

            int pct = (int)MathF.Round(multiplier * 100f);  
            _popup.PopupEntity(  
                Loc.GetString("rat-station-trade-market", 
                    ("finalAmount", finalAmount),
                    ("pct", pct),
                    ("baseAmount", baseAmount)),
                uid, args.User, PopupType.Medium);  

            component.PendingDynamicAmount = finalAmount;  
            TryDispenseItem(uid, component, string.Empty);  
  
            if (virtualItem != null)  
                _virtualItemSystem.DeleteVirtualItem((args.Used, virtualItem), args.User);  
            QueueDel(used);  
            return;  
        } 
        if (TryGetDispenseItem(component, prototype.ID, out string itemId))  
        {  
            args.Handled = true;  
            TryDispenseItem(uid, component, itemId);  
  
            if (virtualItem != null)  
                _virtualItemSystem.DeleteVirtualItem((args.Used, virtualItem), args.User);  
            QueueDel(used);  
        }  
        else  
        {  
            _audioSystem.PlayPvs(component.DenySound, uid);  
        }  
    }  
  
    public bool TryGetDispenseItem(DispenserComponent component, string itemId, out string dispenseItemId)  
    {  
        if (string.IsNullOrEmpty(itemId))  
        {  
            dispenseItemId = string.Empty;  
            return false;  
        }  
  
        foreach (var kvp in component.Inventory)  
        {  
            if (kvp.Key == itemId)  
            {  
                dispenseItemId = kvp.Value;  
                return !string.IsNullOrEmpty(dispenseItemId);  
            }  
        }  
  
        dispenseItemId = string.Empty;  
        return false;  
    }  
  
    public void TryDispenseItem(EntityUid uid, DispenserComponent component, string itemId)  
    {  
        component.Dispensing = true;  
        component.DispensingItemId = itemId;  
        component.DispenseTimer = 0f;  
  
        _audioSystem.PlayPvs(component.DispenseSound, uid);  
    }  
  
    public void Dispense(EntityUid uid, DispenserComponent component, string itemId)  
    { 
        if (component.PendingDynamicAmount > 0)  
        {  
            _stackSystem.SpawnMultiple("SpaceCash", component.PendingDynamicAmount, Transform(uid).Coordinates);  
            component.PendingDynamicAmount = 0;  
            return;  
        }  

        if (!string.IsNullOrEmpty(itemId))  
            Spawn(itemId, Transform(uid).Coordinates);  
    }  
  
    public override void Update(float frameTime)  
    {  
        base.Update(frameTime);  
  
        var query = EntityQueryEnumerator<DispenserComponent>();  
        while (query.MoveNext(out var uid, out var component))  
        {  
            if (!component.Dispensing)  
                continue;  
  
            component.DispenseTimer += frameTime;  
            if (component.DispenseTimer >= component.DispenseTime)  
            {  
                component.DispenseTimer = 0f;  
                component.Dispensing = false;  
  
                Dispense(uid, component, component.DispensingItemId);  
            }  
        }  
    }  
}