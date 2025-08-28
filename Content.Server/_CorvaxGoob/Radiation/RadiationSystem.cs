
using Content.Shared.EntityEffects;
using Content.Shared.Inventory;
using Content.Shared.Radiation.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.Metrics;
using System.Linq;

namespace Content.Server._CorvaxGoob.Radiation;

public sealed class RadiationSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly EntityWhitelistSystem _whiteList = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LivingRadiationReceiverComponent, OnIrradiatedEvent>(OnIrradiated);
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

    public void OnIrradiated(Entity<LivingRadiationReceiverComponent> entity, ref OnIrradiatedEvent ev)
    {
        if (entity.Comp.MinimumRatiadionThreshold > ev.RadsPerSecond)
            return;

        var summaryDamage = ev.RadsPerSecond;

        float protectionMultiplier = 1;

        foreach (var slot in entity.Comp.RequiresSlotsProtection)
        {
            _inventorySystem.TryGetSlotEntity(entity, slot, out var clothing);

            if (TryComp<ClothingRadiationProtectionComponent>(clothing, out var radiationProtection))
            {
                protectionMultiplier -= (1 / (float) entity.Comp.RequiresSlotsProtection.Count);
                summaryDamage *= radiationProtection.ProtectionCoefficient;
            }
        }

        summaryDamage *= protectionMultiplier;

        entity.Comp.CurrentRadiationLevel += summaryDamage;

        if (entity.Comp.CurrentRadiationLevel < entity.Comp.EffectThreshold)
            return;

        entity.Comp.CurrentRadiationLevel = 0;

        var protoList = WhiteListPrototypes(_proto.EnumeratePrototypes<RadiationEffectPrototype>().ToList(), entity);
        var totalWeight = CalculateTotalWeight(protoList);

        for (var i = 0; i < protoList.Count + 1; i++)
        {
            var effect = PickByWeight(protoList, totalWeight);

            if (effect is null)
                continue;

            if (entity.Comp.AppliedEffects.Contains(effect) && !effect.CanRepeat || effect.WhiteList is not null
                && _whiteList.IsBlacklistFail(effect.WhiteList, entity) || effect.Events is null)
            {
                protoList.Remove(effect);
                totalWeight -= effect.Weight;
                continue;
            }

            ApplyRadiationEffect(entity, effect);

            if (!effect.CanRepeat)
                entity.Comp.AppliedEffects.Add(effect);

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

    private List<RadiationEffectPrototype> WhiteListPrototypes(List<RadiationEffectPrototype> list, EntityUid uid)
    {
        var availableProtos = new List<RadiationEffectPrototype>();
        foreach (var effect in list)
        {
            if (effect.WhiteList is not null && _whiteList.IsWhitelistFail(effect.WhiteList, uid))
                continue;

            if (effect.BlackList is not null && _whiteList.IsBlacklistPass(effect.BlackList, uid))
                continue;

            availableProtos.Add(effect);
        }

        return availableProtos;
    }
}
