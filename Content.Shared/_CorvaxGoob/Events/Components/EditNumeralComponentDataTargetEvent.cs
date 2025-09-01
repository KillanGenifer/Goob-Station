namespace Content.Shared._CorvaxGoob.Events.Components;

[Serializable, DataDefinition]
public sealed partial class EditNumeralComponentDataTargetEvent : BaseTargetEvent
{
    [DataField]
    [AlwaysPushInheritance]
    public List<NumeralDataOperation> Operations;
}

[Serializable, DataDefinition]
public sealed partial class NumeralDataOperation
{
    [DataField]
    public string Component;

    [DataField]
    public string Field;

    [DataField]
    public float Value;

    [DataField]
    public string Operation;
}
