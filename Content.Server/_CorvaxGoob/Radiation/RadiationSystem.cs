
using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Radiation.Components;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Radiation.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server._CorvaxGoob.Radiation;

public sealed class RadiationSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly EntityWhitelistSystem _whiteList = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    private float _damageMultiplier = 5;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadiationReceiverComponent, OnIrradiatedEvent>(OnIrradiated);
        SubscribeLocalEvent<LivingRadiationReceiverComponent, DamageChangedEvent>(OnDamage);
    }

    public void ApplyRadiationEffect(EntityUid uid, RadiationEffectPrototype effect)
    {
        if (effect.Events is null)
            return;

        foreach (var targetEvent in effect.Events)
        {
            targetEvent.Target = uid;
            RaiseLocalEvent(uid, (object) targetEvent, true);
        }
    }

    private void OnDamage(Entity<LivingRadiationReceiverComponent> entity, ref DamageChangedEvent ev)
    {
        if (!ev.DamageIncreased)
            return;

        if (ev.DamageDelta is null)
            return;

        if (!ev.DamageDelta.DamageDict.TryGetValue("Radiation", out var radDamage))
            return;

        DamageSpecifier newDamage = new()
        {
            DamageDict = new()
            {
                {"Radiation", -radDamage}
            }
        };

        _damageable.TryChangeDamage(entity, newDamage);

        Irradiate(entity, radDamage.Float() * _damageMultiplier);
    }

    public void OnIrradiated(Entity<RadiationReceiverComponent> entity, ref OnIrradiatedEvent ev)
    {
        if (HasComp<LivingRadiationReceiverComponent>(entity))
        {
            Irradiate(entity, ev.RadsPerSecond);
            return;
        }

        if (!TryComp<DamageableComponent>(entity, out var damageable))
            return;

        var damageValue = FixedPoint2.New(ev.TotalRads);

        DamageSpecifier damage = new();
        foreach (var typeId in damageable.RadiationDamageTypeIDs)
        {
            damage.DamageDict.Add(typeId, damageValue);
        }
        _damageable.TryChangeDamage(entity, damage, interruptsDoAfters: false, origin: ev.Origin);

    }

    public void Irradiate(EntityUid uid, float radiation)
    {
        if (!TryComp<LivingRadiationReceiverComponent>(uid, out var radiationReceiver))
            return;

        if (!radiationReceiver.WorksOnDead && TryComp<MobThresholdsComponent>(uid, out var mobThresholds) && mobThresholds.CurrentThresholdState == Shared.Mobs.MobState.Dead)
            return;

        radiation *= radiationReceiver.RadiationResistance;

        if (radiationReceiver.MinimumRatiadionThreshold > radiation)
            return;

        float protectionMultiplier = 1;

        foreach (var slot in radiationReceiver.RequiresSlotsProtection)
        {
            _inventorySystem.TryGetSlotEntity(uid, slot, out var clothing);

            if (TryComp<ClothingRadiationProtectionComponent>(clothing, out var radiationProtection))
            {
                protectionMultiplier -= (1 / (float) radiationReceiver.RequiresSlotsProtection.Count);
                radiation *= radiationProtection.ProtectionCoefficient;
            }
        }

        radiation *= protectionMultiplier;

        radiationReceiver.CurrentRadiationThreshold += radiation;

        if (radiationReceiver.CurrentRadiationThreshold < radiationReceiver.EffectThreshold)
            return;

        radiationReceiver.CurrentRadiationLevel++;
        radiationReceiver.CurrentRadiationThreshold = 0;

        if (radiationReceiver.CurrentRadiationLevel >= radiationReceiver.CriticalRadiationLevel)
            _damageable.TryChangeDamage(uid, radiationReceiver.CriticalRadiationDamage);

        var protoList = WhiteListPrototypes(_proto.EnumeratePrototypes<RadiationEffectPrototype>().ToList(), (uid, radiationReceiver));
        var totalWeight = CalculateTotalWeight(protoList);

        for (var i = 0; i < protoList.Count + 1; i++)
        {
            var effect = PickByWeight(protoList, totalWeight);

            if (effect is null)
                continue;

            if (radiationReceiver.AppliedEffects.Contains(effect) && !effect.CanRepeat || effect.Events is null)
            {
                protoList.Remove(effect);
                totalWeight -= effect.Weight;
                continue;
            }

            ApplyRadiationEffect(uid, effect);

            if (!effect.CanRepeat)
                radiationReceiver.AppliedEffects.Add(effect);

            return;
        }
    }

    private RadiationEffectPrototype? PickByWeight(List<RadiationEffectPrototype> list, int totalWeight)
    {
        if (list.Count == 0)
            return null;

        if (list.Count == 1)
            return list[0];

        double randomValue = _random.NextDouble() * totalWeight;
        double cumulativeWeight = 0;

        foreach (var effect in list)
        {
            cumulativeWeight += effect.Weight;
            if (randomValue <= cumulativeWeight)
                return effect;
        }

        return list[^1];
    }

    private int CalculateTotalWeight(List<RadiationEffectPrototype> list)
    {
        var totalWeight = 0;
        foreach (var effect in list)
            totalWeight += effect.Weight;

        return totalWeight;
    }

    private List<RadiationEffectPrototype> WhiteListPrototypes(List<RadiationEffectPrototype> list, Entity<LivingRadiationReceiverComponent> entity)
    {
        var availableProtos = new List<RadiationEffectPrototype>();
        foreach (var effect in list)
        {
            if (effect.WhiteList is not null && _whiteList.IsWhitelistFail(effect.WhiteList, entity))
                continue;

            if (effect.BlackList is not null && _whiteList.IsBlacklistPass(effect.BlackList, entity))
                continue;

            if (effect.RequiredRadiationLevel > entity.Comp.CurrentRadiationLevel)
                continue;

            availableProtos.Add(effect);
        }

        return availableProtos;
    }
}
