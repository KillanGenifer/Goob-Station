using Content.Shared.Damage;
using Content.Shared.Weather;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._CorvaxGoob.StationEvents.Events;

[RegisterComponent]
public sealed partial class RadiationStormComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string MaintenanceMarker = "MaintenanceAreaMarker";

    public List<EntityUid> StormEntities = new List<EntityUid>();

    [DataField]
    public SoundSpecifier AlarmAudio = new SoundPathSpecifier("/Audio/_CorvaxGoob/Announcements/bloblarm.ogg");

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<WeatherPrototype>))]
    public string WeatherPrototype = "RadiationStorm";

    [DataField]
    public EntityUid? AttachedStation;

    [DataField]
    public float MinTimeBeforeAlert = 5;

    [DataField]
    public float MaxTimeBeforeAlert = 20;

    [DataField]
    public TimeSpan? TimeBeforeAlert;

    [DataField]
    public TimeSpan? SpawnStormAt;

    [DataField]
    public TimeSpan StormTimeAfterAlert = TimeSpan.FromSeconds(7);

    [DataField]
    public TimeSpan DamageTime = TimeSpan.MinValue;

    [DataField]
    public float DamageDelay = 5f;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            {"Radiation", 5}
        }
    };
}
