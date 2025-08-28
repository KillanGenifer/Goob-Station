namespace Content.Server._CorvaxGoob.Radiation;

[RegisterComponent]
public sealed partial class ClothingRadiationProtectionComponent : Component
{
    [DataField]
    public float ProtectionCoefficient = 0.5f;
}
