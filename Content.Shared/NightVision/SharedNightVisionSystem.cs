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

        if (ent.Comp.Action is null)
            return;

        ent.Comp.ActionEntity = _actions.AddAction(ent, ent.Comp.Action);
    }

    [SubscribeLocalEvent]
    private void OnRemove(Entity<NightVisionComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.RelayOverlay)
            return;

        RefreshOverlay(ent);

        if (ent.Comp.Action is null)
            return;

        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    [SubscribeLocalEvent]
    private void OnCompEquip(Entity<NightVisionComponent> ent, ref GotEquippedEvent args)
    {
        if (!ent.Comp.RelayOverlay)
            return;

        RefreshOverlay(args.EquipTarget);

        if (ent.Comp.Action is null)
            return;

        ent.Comp.ActionEntity = _actions.AddAction(args.EquipTarget, ent.Comp.Action);
    }

    [SubscribeLocalEvent]
    private void OnCompUnequip(Entity<NightVisionComponent> ent, ref GotUnequippedEvent args)
    {
        if (!ent.Comp.RelayOverlay)
            return;

        RefreshOverlay(args.EquipTarget);

        if (ent.Comp.Action is null)
            return;

        _actions.RemoveAction(args.EquipTarget, ent.Comp.ActionEntity);
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

        args.Components.Add(ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnToggleNightVisionEvent(Entity<NightVisionComponent> ent, ref ToggleNightVisionEvent args)
    {
        SetEnabled(ent.Owner, !ent.Comp.Enabled);
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

        RefreshOverlay(ent);
    }

    protected virtual void RefreshOverlay(EntityUid entity) { }
}

[ByRefEvent]
public record struct RefreshNightVisionEvent() : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
    public List<NightVisionComponent> Components = new();
}
