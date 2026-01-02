namespace Content.Server._CorvaxGoob.BSA;

[RegisterComponent]
public sealed partial class BSAPartComponent : Component
{
    [DataField]
    public BSAPartType PartType = BSAPartType.None;
}

public enum BSAPartType
{
    FuelChamber,
    PowerBox,
    Emitter,
    None
}
