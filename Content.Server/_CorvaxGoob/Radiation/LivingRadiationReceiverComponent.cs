namespace Content.Server._CorvaxGoob.Radiation;

[RegisterComponent]
public sealed partial class LivingRadiationReceiverComponent : Component
{
    [DataField]
    public float MinimumRatiadionThreshold = 0.5f;

    [DataField]
    public float EffectThreshold = 100;

    [DataField]
    public float CurrentRadiationThreshold = 0;

    [DataField]
    public float CurrentRadiationLevel = 1;

    [DataField]
    public HashSet<RadiationEffectPrototype> AppliedEffects = new();

    [DataField]
    public List<string> RequiresSlotsProtection = ["head", "outerClothing"];
}
