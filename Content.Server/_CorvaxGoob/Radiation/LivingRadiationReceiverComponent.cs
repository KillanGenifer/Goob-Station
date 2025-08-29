using Content.Shared.Damage;

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
    public float CriticalRadiationLevel = 10;

    [DataField]
    public bool WorksOnDead = false;

    [DataField]
    public DamageSpecifier CriticalRadiationDamage = new()
    {
        DamageDict = new()
        {
            {"Toxin", 5},
            { "Cellular", 5 }
        }
    };

    [DataField]
    public HashSet<RadiationEffectPrototype> AppliedEffects = new();

    [DataField]
    public List<string> RequiresSlotsProtection = ["head", "outerClothing"];
}
