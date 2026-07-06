using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;

namespace Content.Shared.NightVision;

/// <summary>
/// Shows/hides the <see cref="NightVisionOverlay"/> based on whether the observed
/// entity has a <see cref="NightVisionComponent"/> equipped.
/// </summary>
public abstract partial class SharedNightVisionSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<NightVisionComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.RelayOverlay)
            return;

        RefreshOverlay(ent);
        ent.Comp.ActionEntity = _actions.AddAction(ent, ent.Comp.Action);
    }

    [SubscribeLocalEvent]
    private void OnRemove(Entity<NightVisionComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.RelayOverlay)
            return;

        RefreshOverlay(ent);
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    [SubscribeLocalEvent]
    private void OnCompEquip(Entity<NightVisionComponent> ent, ref GotEquippedEvent args)
    {
        if (!ent.Comp.RelayOverlay)
            return;

        RefreshOverlay(args.EquipTarget);
        ent.Comp.Viewer = args.EquipTarget;
        ent.Comp.ActionEntity = _actions.AddAction(args.EquipTarget, ent.Comp.Action, ent);
    }

    [SubscribeLocalEvent]
    private void OnCompUnequip(Entity<NightVisionComponent> ent, ref GotUnequippedEvent args)
    {
        if (!ent.Comp.RelayOverlay)
            return;

        RefreshOverlay(args.EquipTarget);
        ent.Comp.Viewer = null;
    }

    [SubscribeLocalEvent]
    protected virtual void OnRefreshEquipmentHud(Entity<NightVisionComponent> ent, ref InventoryRelayedEvent<RefreshNightVisionEvent> args)
    {
        OnRefreshComponentHud(ent, ref args.Args);
    }

    [SubscribeLocalEvent]
    protected virtual void OnRefreshComponentHud(Entity<NightVisionComponent> ent, ref RefreshNightVisionEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        args.Entities.Add(ent);
    }

    [SubscribeLocalEvent]
    private void OnToggleNightVisionEvent(ToggleNightVisionEvent args)
    {
        var ent = args.Action.Comp.Container;

        if (!TryComp<NightVisionComponent>(ent, out var nightVisionComp))
            return;

        SetEnabled(ent.Value, !nightVisionComp.Enabled);
        args.Handled = true;
    }

    /// <summary>
    /// Enables or disables the component.
    /// </summary>
    public void SetEnabled(Entity<NightVisionComponent?> ent, bool enabled)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent);
        RefreshOverlay(ent.Comp.Viewer ?? ent);
    }

    protected virtual void RefreshOverlay(EntityUid entity) { }
}

[ByRefEvent]
public record struct RefreshNightVisionEvent() : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
    public List<Entity<NightVisionComponent>> Entities = new();
}
