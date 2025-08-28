using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxGoob.Events;

[Serializable, DataDefinition]
public sealed partial class ChangeComponentTargetEvent : BaseTargetEvent
{
    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry ToAdd = new();

    [DataField]
    [AlwaysPushInheritance]
    public HashSet<string> ToRemove = new();
}
