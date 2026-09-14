using Content.Shared._ES.Viewcone.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Funkystation.Viewcone;

/// <summary>
/// Handles client-side predictions
/// </summary>
public sealed partial class ViewconeStorageBlindSystem : EntitySystem
{
    [Dependency] private INetManager _net = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityStorageComponent, StorageAfterCloseEvent>(OnStorageClosed);
        SubscribeLocalEvent<EntityStorageComponent, StorageAfterOpenEvent>(OnStorageOpened);
        SubscribeLocalEvent<EntityStorageComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<EntityStorageComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    // mark as blind when closed
    private void OnStorageClosed(Entity<EntityStorageComponent> ent, ref StorageAfterCloseEvent _)
    {
        MarkBlindByContainer(ent);
    }

    // unmark
    private void OnStorageOpened(Entity<EntityStorageComponent> ent, ref StorageAfterOpenEvent _)
    {
        UnmarkBlindByContainer(ent);
    }

    // in case something is somehow inserted while still closed
    private void OnInserted(Entity<EntityStorageComponent> ent, ref EntInsertedIntoContainerMessage _)
    {
        MarkBlindByContainer(ent);
    }

    // and removes marker when something leaves storage
    private void OnRemoved(Entity<EntityStorageComponent> ent, ref EntRemovedFromContainerMessage _)
    {
        UnmarkBlindByContainer(ent);
    }

    /// <summary>
    /// Marks all entities inside the container `ent`
    /// as blinded by a storage
    /// </summary>
    private void MarkBlindByContainer(Entity<EntityStorageComponent> ent)
    {
        // Client side prediction
        RaiseLocalEvent(ent, new ViewconeStorageClosedEvent());

        // We prevent the client from changing the server-autoritative component
        if (_net.IsClient)
            return;

        if (ent.Comp.Contents is not { } contents)
            return;

        foreach (var contained in contents.ContainedEntities)
        {
            // Ignore all entities that do not posses a ViewconeBlindnessComponent
            if (!TryComp<ViewconeBlindnessComponent>(contained, out var comp))
                continue;

            comp.IsBlind = true;
            comp.Reason &= ViewconeBlindnessReason.Storage;

            // Dirty the component so the event `AfterAutoHandleStateEvent`
            // gets called once both server and client are reconciled
            // it then gets handled in ViewconeBlindSystem.cs
            Dirty(contained, comp);
        }
    }

    /// <summary>
    /// Unmark all entities inside the container `ent`
    /// as blinded by a storage
    /// </summary>
    private void UnmarkBlindByContainer(Entity<EntityStorageComponent> ent)
    {
        // Client side prediction
        // see above
        RaiseLocalEvent(ent, new ViewconeStorageOpenedEvent());

        if (!_net.IsClient)
            return;

        if (ent.Comp.Contents is not { } contents)
            return;

        foreach (var contained in contents.ContainedEntities)
        {
            if (!TryComp<ViewconeBlindnessComponent>(contained, out var comp))
                continue;

            comp.IsBlind = false;
            comp.Reason |= ViewconeBlindnessReason.Storage;

            Dirty(contained, comp);
        }
    }
}
