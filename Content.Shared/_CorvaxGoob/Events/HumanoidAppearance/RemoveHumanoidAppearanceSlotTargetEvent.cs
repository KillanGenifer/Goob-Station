using Content.Shared.Humanoid.Markings;

namespace Content.Shared._CorvaxGoob.Events.HumanoidAppearance;

[Serializable, DataDefinition]
public sealed partial class RemoveHumanoidAppearanceSlotTargetEvent : BaseTargetEvent
{
    [DataField]
    [AlwaysPushInheritance]
    public MarkingCategories Category;

    [DataField]
    [AlwaysPushInheritance]
    public int Slot;
}
