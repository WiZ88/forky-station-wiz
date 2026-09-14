using Content.Shared._ES.Viewcone.Components;
using Content.Shared._Funkystation.Viewcone;
using Content.Shared.Disposal.Unit;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Network;

namespace Content.Shared._ES.Viewcone;

/// <summary>
///     Public API for getting the actual modified viewcone angle (including equipment etc) rather than just the base angle
/// </summary>
public sealed class ESViewconeAngleSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private InventorySystem _inv = default!;

    private const float LerpHalfLife = 0.1f;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESViewconeModifierComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ESViewconeModifierComponent, ESViewconeGetAngleModifierEvent>(OnAngleModify);
        SubscribeLocalEvent<ESViewconeModifierComponent, InventoryRelayedEvent<ESViewconeGetAngleModifierEvent>>(OnAngleInventoryModify);
        SubscribeLocalEvent<ESViewconeModifierComponent, StatusEffectRelayedEvent<ESViewconeGetAngleModifierEvent>>(OnAngleStatusEffectModify);

        SubscribeLocalEvent<ViewconeStorageBlindComponent, ESViewconeGetAngleModifierEvent>(OnConcealedAngle); // Funky
        SubscribeLocalEvent<BeingDisposedComponent, ESViewconeGetAngleModifierEvent>(OnBeingDisposedAngle);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_net.IsClient)
            return;

        var enumerator = AllEntityQuery<ESViewconeComponent>();
        while (enumerator.MoveNext(out var ent, out var viewcone))
        {
            if (viewcone.DesiredConeAngle.Equals(viewcone.CurrentConeAngle))
                return;

            viewcone.CurrentConeAngle = MathHelper.Lerp(viewcone.CurrentConeAngle,
                viewcone.DesiredConeAngle,
                1f - MathF.Pow(2f, -(frameTime / 0.01f)));
        }
    }

    private void OnExamined(Entity<ESViewconeModifierComponent> ent, ref ExaminedEvent args)
    {
        var loc = "es-viewcone-modifier-examine-increase";
        if (ent.Comp.AngleModifier < 0)
            loc = "es-viewcone-modifier-examine-decrease";

        var degrees = (int) MathF.Abs(ent.Comp.AngleModifier);
        args.PushMarkup(Loc.GetString(loc, ("degrees", degrees)));
    }

    private void OnAngleModify(Entity<ESViewconeModifierComponent> ent, ref ESViewconeGetAngleModifierEvent args)
    {
        if (_net.IsClient && args.Source is not null)
        {
            var ev = new ViewconeAngleEvent(ent.Comp.AngleModifier);
            RaiseLocalEvent(args.Source.Value, ev, true);
            return;
        }
        args.ModifyAngle(ent.Comp.AngleModifier);
    }

    private void OnAngleInventoryModify(Entity<ESViewconeModifierComponent> ent, ref InventoryRelayedEvent<ESViewconeGetAngleModifierEvent> args)
    {
        if (_net.IsClient && args.Args.Source is not null)
        {
            var ev = new ViewconeAngleEvent(ent.Comp.AngleModifier);
            RaiseLocalEvent(args.Args.Source.Value, ev, true);
            return;
        }
        args.Args.ModifyAngle(ent.Comp.AngleModifier);
    }

    private void OnAngleStatusEffectModify(Entity<ESViewconeModifierComponent> ent, ref StatusEffectRelayedEvent<ESViewconeGetAngleModifierEvent> args)
    {
        args.Args.ModifyAngle(ent.Comp.AngleModifier);
    }

    // Funky start
    private void OnConcealedAngle(Entity<ViewconeStorageBlindComponent> ent, ref ESViewconeGetAngleModifierEvent args)
    {
        args.ModifyAngle(-360f);
    }
    // Funky end

    private void OnBeingDisposedAngle(Entity<BeingDisposedComponent> ent, ref ESViewconeGetAngleModifierEvent args)
    {
        args.ModifyAngle(-360f);
    }

    /// <summary>
    ///     Returns the modified viewcone angle for an entity, calculated from the base, taking into account
    ///     equipment & status effects & whatnot
    /// </summary>
    public float GetModifiedViewconeAngle(Entity<ESViewconeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return 0f;

        ent.Comp.LastConeAngleModifierSeen = 0f;

        var ev = new ESViewconeGetAngleModifierEvent(ent.Owner);
        RaiseLocalEvent(ent, ref ev, true);

        if (ent.Comp.IsBlind)
            ent.Comp.DesiredConeAngle = -10f;
        else
            ent.Comp.DesiredConeAngle = ent.Comp.BaseConeAngle + ent.Comp.LastConeAngleModifierSeen;

        return ent.Comp.CurrentConeAngle;
    }

    public float GetModifiedConeIgnoreRadius(Entity<ESViewconeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return 0f;

        if (ent.Comp.IsBlind)
            return ent.Comp.ConeIgnoreRadiusBlind;

        return ent.Comp.ConeIgnoreRadius;
    }
}
