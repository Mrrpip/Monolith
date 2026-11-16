using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared._FinalHorizon.Guns.AlternateGun;

public sealed partial class AlternateGunSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handSystem = default!;
    [Dependency] private readonly SharedAudioSystem _sound = default!;
    [Dependency] private  SharedPopupSystem _popup = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AlternateGunAmmoProviderComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<AlternateGunComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<AlternateGunComponent, GetVerbsEvent<Verb>>(OnGetVerbs);


        CommandBinds.Builder
            .Bind(ContentKeyFunctions.MonoSwapUnderbarrel,
                InputCmdHandler.FromDelegate(HandleToggleUnderbarrel, handle: false))
            .Register<AlternateGunSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<AlternateGunSystem>();
    }

    private void HandleToggleUnderbarrel(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { } user)
            return;

        if (_handSystem.GetActiveItem(user) is not { } heldEntity)
            return;

        if (!TryComp<AlternateGunComponent>(heldEntity, out var comp))
            return;

        TrySwap(heldEntity, comp, user);
    }


    private void OnGetVerbs(EntityUid uid, AlternateGunComponent comp, ref GetVerbsEvent<Verb> args)
    {

        var user = args.User;

        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        args.Verbs.Add(new InteractionVerb
        {
            Act = () => TrySwap(uid, comp,  user),
            Text = "Toggle Underbarrel"
        });
    }

    private void TrySwap(EntityUid uid, AlternateGunComponent comp, EntityUid user)
    {
        if (comp.Enabled)
        {
            _popup.PopupEntity(Loc.GetString(comp.DisableMessage), user, user);
            _sound.PlayPredicted(comp.SwitchSound, uid, user);
            comp.Enabled = false;
            if (TryComp<UseDelayComponent>(uid, out var useDelay))
            {
                _useDelay.TryResetDelay((uid, useDelay), true);
            }
        }
        else
        {
            _popup.PopupEntity(Loc.GetString(comp.EnableMessage), user, user);
            _sound.PlayPredicted(comp.SwitchSound, uid, user);
            comp.Enabled = true;
            if (TryComp<UseDelayComponent>(uid, out var useDelay))
            {
                _useDelay.TryResetDelay((uid, useDelay), true);
            }
        }
    }

    private void OnExamine(EntityUid uid, AlternateGunComponent comp, ref ExaminedEvent args)
    {
        var msg = Loc.GetString(comp.DisabledMessage);
        if (comp.Enabled)
            msg = Loc.GetString(comp.EnabledMessage);
        args.PushMessage(FormattedMessage.FromMarkupOrThrow(msg), 5);
    }

    private void OnTakeAmmo(EntityUid uid, AlternateGunAmmoProviderComponent comp, ref TakeAmmoEvent args)
    {

        if (args.User == null)
            return;
        var held = _handSystem.GetActiveItem(args.User.Value);
        if (held == null || !TryComp<ItemSlotsComponent>(held, out var slots) || slots == null)
            return;

        var ammoEnt = _itemSlots.GetItemOrNull(held.Value, comp.Slot);

        if (ammoEnt != null)
            args.Ammo.Add((ammoEnt, _gunSystem.EnsureShootable(ammoEnt.Value)));

    }

    public Entity<GunComponent> CheckForAltGun(Entity<GunComponent> gun)
    {
        if (!TryComp<AlternateGunComponent>(gun, out var comp) || !comp.Enabled)
            return gun;

        var ent = _itemSlots.GetItemOrNull(gun, comp.Slot);

        if (ent != null && TryComp<GunComponent>(ent, out var altGunComp))
        {
            return (ent.Value, altGunComp);
        }
        return gun;
    }
}
