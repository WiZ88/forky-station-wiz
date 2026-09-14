using System.Linq;
using Content.Shared._ES.Viewcone.Components;
using Content.Shared._Funkystation.Viewcone;
using Content.Shared.Storage.Components;
using Robust.Client.Player;

namespace Content.Client._Funkystation.Overlays;

/// <summary>
/// Handles viewcone blindness client prediction
/// Also see <see cref="ESViewconeComponent"/>
/// </summary>
public sealed partial class ViewconeBlindSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;

    /// <summary>
    /// Client handler for <see cref="ViewconeStorageClosedEvent"/>.
    /// Predicts the state of <see cref="ESViewconeComponent.IsBlind"/>.
    /// The server state gets reconciled with <see cref="OnViewconeStorageClosedAfterEvent"/>.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnViewconeStorageClosedEvent(Entity<ViewconeBlindnessComponent> ent, ref ViewconeStorageClosedEvent args)
    {
        if (ent != _player.LocalEntity)
            return;

        if (!TryComp<ESViewconeComponent>(ent, out var comp))
            return;

        comp.IsBlind = true;
    }

    /// <summary>
    /// Client handler for <see cref="ViewconeStorageOpenedEvent"/>.
    /// Predicts the state of <see cref="ESViewconeComponent.IsBlind"/>.
    /// The server state gets reconciled with <see cref="OnViewconeStorageOpenedAfterEvent"/>.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnViewconeStorageOpenedEvent(Entity<ViewconeBlindnessComponent> ent, ref ViewconeStorageOpenedEvent args)
    {
        if (ent != _player.LocalEntity)
            return;

        if (!TryComp<ESViewconeComponent>(ent, out var comp))
            return;

        comp.IsBlind = false;
    }

    [SubscribeLocalEvent]
    private void OnAfterState(Entity<ViewconeBlindnessComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<ESViewconeComponent>(ent, out var comp))
            return;

        // Reconcile with the server state
        // ESViewconeComponent is client predicted
        // ViewconeBlindnessComponent is server authoritative
        comp.IsBlind = ent.Comp.IsBlind;
    }
}
