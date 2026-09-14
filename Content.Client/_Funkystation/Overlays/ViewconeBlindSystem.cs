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
    private void OnViewconeStorageClosedEvent(Entity<EntityStorageComponent> ent, ref ViewconeStorageClosedEvent args)
    {
        if (ent.Comp.Contents is not { } contents)
            return;

        foreach (var contained in contents.ContainedEntities)
        {
            if (contained != _player.LocalEntity)
                continue;

            if (!TryComp<ESViewconeComponent>(contained, out var comp))
                return;

            comp.IsBlind = true;
        }
    }

    /// <summary>
    /// Client handler for <see cref="ViewconeStorageOpenedEvent"/>.
    /// Predicts the state of <see cref="ESViewconeComponent.IsBlind"/>.
    /// The server state gets reconciled with <see cref="OnViewconeStorageOpenedAfterEvent"/>.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnViewconeStorageOpenedEvent(Entity<EntityStorageComponent> ent, ref ViewconeStorageOpenedEvent args)
    {
        if (ent.Comp.Contents is not { } contents)
            return;

        var isEntityInsideStorage = contents.ContainedEntities.Any(contained => contained == _player.LocalEntity);

        // If the player entity is inside the storage
        // it means the state has been predicted correctly
        if (isEntityInsideStorage)
            return;

        // Predicted incorrectly, revert
        if (!TryComp<ESViewconeComponent>(_player.LocalEntity, out var comp))
            return;

        comp.IsBlind = false;
    }

    [SubscribeLocalEvent]
    private void OnAfterState(Entity<ViewconeBlindnessComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<ESViewconeComponent>(_player.LocalEntity, out var comp))
            return;

        // Reconcile with the server state
        // ESViewconeComponent is client predicted
        // ViewconeBlindnessComponent is server authoritative
        comp.IsBlind = ent.Comp.IsBlind;
    }
}
