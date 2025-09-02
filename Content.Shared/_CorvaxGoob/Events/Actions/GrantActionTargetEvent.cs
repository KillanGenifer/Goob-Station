namespace Content.Shared._CorvaxGoob.Events.Actions;

[Serializable, DataDefinition]
public sealed partial class GrantActionTargetEvent : BaseTargetEvent
{
    [DataField]
    [AlwaysPushInheritance]
    public string Action;
}
