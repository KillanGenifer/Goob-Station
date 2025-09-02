namespace Content.Server._CorvaxGoob.Airlock;

[RegisterComponent]
public sealed partial class TimedAirlockStatusComponent : Component
{
    [DataField]
    public AirlockStatus AirlockStatus = AirlockStatus.Emergency;

    [DataField]
    public TimeSpan SelfDeleteAt = TimeSpan.MaxValue;
}

public enum AirlockStatus
{
    Emergency = 0,
    Bolts = 1,
}
