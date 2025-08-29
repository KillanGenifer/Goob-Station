using Content.Shared._CorvaxGoob.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._CorvaxGoob.Radiation;

[Prototype("radiationEffect")]
public sealed class RadiationEffectPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private init; } = default!;

    [DataField]
    public bool CanRepeat = false;

    [DataField]
    public EntityWhitelist? WhiteList = default!;

    [DataField]
    public EntityWhitelist? BlackList = default!;

    [DataField]
    public List<BaseTargetEvent>? Events;

    [DataField]
    public int Weight = 1;

    [DataField]
    public int RequiredRadiationLevel = 1;
}
