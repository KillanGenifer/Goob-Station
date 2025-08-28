
using Content.Shared.EntityEffects;
using Content.Shared.Inventory;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LivingRadiationReceiverComponent, OnIrradiatedEvent>(OnIrradiated);
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

        var protoList = _proto.EnumeratePrototypes<RadiationEffectPrototype>().ToList();

        for (var i = 0; i < protoList.Count + 1; i++)
        {
            var effect = _random.Pick(protoList);

            if (entity.Comp.AppliedEffects.Contains(effect) && !effect.CanRepeat)
            {
                protoList.Remove(effect);
                continue;

            }

            if (effect.WhiteList is not null && _whiteList.IsBlacklistFail(effect.WhiteList, entity) || effect.Events is null)
            {
                protoList.Remove(effect);
                continue;
            }

            foreach (var targetEvent in effect.Events)
            {
                targetEvent.Target = entity;
                RaiseLocalEvent(entity, (object) targetEvent, true);
            }

            if (!effect.CanRepeat)
                entity.Comp.AppliedEffects.Add(effect);

            return;
        }
    }
}
