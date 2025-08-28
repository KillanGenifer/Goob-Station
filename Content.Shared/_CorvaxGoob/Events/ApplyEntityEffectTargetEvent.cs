using Content.Shared.EntityEffects;

namespace Content.Shared._CorvaxGoob.Events;

[Serializable, DataDefinition]
public sealed partial class ApplyEntityEffectTargetEvent : BaseTargetEvent
{
    [DataField]
    [AlwaysPushInheritance]
    public List<EntityEffect> Effects;
}
