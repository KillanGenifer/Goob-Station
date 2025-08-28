using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxGoob.Events;

[Serializable, DataDefinition]
public sealed partial class ApplyEntityEffectTargetEvent : BaseTargetEvent
{
    [DataField]
    [AlwaysPushInheritance]
    public List<EntityEffect> Effects;
}
