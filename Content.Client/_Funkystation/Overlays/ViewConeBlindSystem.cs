using Content.Shared._ES.Viewcone.Components;
using Content.Shared._Funkystation.Viewcone;
using Content.Shared.Storage.Components;
using Robust.Client.Player;

namespace Content.Client._Funkystation.Overlays;

public sealed partial class ViewConeBlindSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityStorageComponent, EntityEnteredStorageEvent>(OnInsertedIntoContainer);
        SubscribeLocalEvent<EntityStorageComponent, EntityExitedStorageEvent>(OnRemovedFromContainer);
    }

    private void OnInsertedIntoContainer(Entity<EntityStorageComponent> ent, ref EntityEnteredStorageEvent args)
    {
        if (ent.Comp.Contents is not { } contents)
            return;

        foreach (var contained in contents.ContainedEntities)
        {
            if (contained != _player.LocalEntity)
                return;

            if (!TryComp<ESViewconeComponent>(contained, out var comp))
                return;

            comp.IsBlind = true;
        }
    }

    private void OnRemovedFromContainer(Entity<EntityStorageComponent> ent, ref EntityExitedStorageEvent args)
    {
        if (ent.Comp.Contents is not { } contents)
            return;

        var found = false;

        foreach (var contained in contents.ContainedEntities)
        {
            if (contained == _player.LocalEntity)
                found = true;
        }

        if (found)
            return;

        if (!TryComp<ESViewconeComponent>(_player.LocalEntity, out var comp))
            return;

        comp.IsBlind = false;
    }
}
