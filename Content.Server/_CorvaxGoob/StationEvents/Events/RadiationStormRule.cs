using Content.Server._CorvaxGoob.Airlock;
using Content.Server.Access.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Radiation.Components;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Server.Weather;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Maps;
using Content.Shared.Station.Components;
using Content.Shared.Weather;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CorvaxGoob.StationEvents.Events;

public sealed class RadiationStormRule : StationEventSystem<RadiationStormComponent>
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly WeatherSystem _weatherSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    protected override void Added(EntityUid uid, RadiationStormComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        component.TimeBeforeAlert = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(component.MinTimeBeforeAlert, component.MaxTimeBeforeAlert));
    }

    protected override void Started(EntityUid uid, RadiationStormComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        SetAirlocksEmergencyLock(component, stationEvent, true);
    }

    protected override void ActiveTick(EntityUid uid, RadiationStormComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        DoPreSpawnTimeChecks(uid, component);
        DoRadstormTimeChecks(uid, component);
    }

    private void DoRadstormTimeChecks(EntityUid uid, RadiationStormComponent component)
    {
        if (component.DamageTime < _timing.CurTime)
        {
            component.DamageTime = _timing.CurTime + TimeSpan.FromSeconds(component.DamageDelay);

            var query = _entityManager.EntityQueryEnumerator<RadiationReceiverComponent>();
            while (query.MoveNext(out var receiverUid, out var receiverComponent))
            {
                if (Transform(receiverUid).GridUid == null) // if entity in space
                {
                    _damageable.TryChangeDamage(receiverUid, component.Damage);
                    return;
                }
                else
                    foreach (var entity in _lookupSystem.GetEntitiesInRange(receiverUid, 0.5f)) // checking in tile range for radstorm
                    {
                        var entityProto = MetaData(entity).EntityPrototype;
                        if (entityProto is not null && entityProto.ID == component.RadstormPrototype)
                            _damageable.TryChangeDamage(receiverUid, component.Damage);
                        return;
                    }
            }
        }
    }
    private void DoPreSpawnTimeChecks(EntityUid uid, RadiationStormComponent component)
    {
        if (component.TimeBeforeAlert is not null && component.TimeBeforeAlert < _timing.CurTime)
        {
            if (!TryGetRandomStation(out var stationUid))
                return;

            _chat.DispatchStationAnnouncement(stationUid.Value, "Пиздец сука нахуй", announcementSound: component.AlarmAudio, colorOverride: Color.Red);

            component.TimeBeforeAlert = null;
            component.SpawnStormAt = _timing.CurTime + component.StormTimeAfterAlert;
        }

        if (component.SpawnStormAt is not null && component.SpawnStormAt < _timing.CurTime)
        {
            if (!TryGetRandomStation(out var stationUid))
                return;

            if (!TryComp<StationDataComponent>(stationUid, out var stationData))
                return;

            if (!TryComp<StationEventComponent>(uid, out var stationEvent))
                return;

            var stationGrid = GetStationMainGrid(stationData);

            SpawnStormTiles(component);
            if (stationGrid.HasValue)
                _weatherSystem.SetWeather(Transform(stationGrid.Value).MapID, _prototypeManager.Index<WeatherPrototype>(component.WeatherPrototype), stationEvent.EndTime);

            component.SpawnStormAt = null;
        }
    }

    private void SpawnStormTiles(RadiationStormComponent component)
    {
        if (!TryGetRandomStation(out var chosenStation))
            return;

        if (!TryComp<StationDataComponent>(chosenStation, out var stationData))
            return;

        var stationGrid = GetStationMainGrid(stationData);

        if (!TryComp<MapGridComponent>(stationGrid, out var mapGrid))
            return;

        if (!stationGrid.HasValue)
            return;

        foreach (var tile in _map.GetAllTiles(stationGrid.Value, mapGrid))
        {
            if (tile.IsSpace())
                continue;

            var isMaints = false;
            var tileCenter = _turf.GetTileCenter(tile);

            foreach (var entity in _lookupSystem.GetEntitiesInRange(tileCenter, 0.5f))
            {
                var metadata = MetaData(entity);

                if (metadata.EntityPrototype is not null && metadata.EntityPrototype.ID == component.MaintenanceMarker && Transform(entity).Anchored)
                {
                    isMaints = true;
                    break;
                }
            }

            if (!isMaints)
                component.StormEntities.Add(Spawn(component.RadstormPrototype, tileCenter));
        }
    }

    private void SetAirlocksEmergencyLock(RadiationStormComponent rule, StationEventComponent stationEvent, bool unlocked)
    {
        if (!TryGetRandomStation(out var station))
            return;

        var locations = EntityQueryEnumerator<AirlockComponent, AccessReaderComponent, TransformComponent>();
        var autoLockTime = stationEvent.Duration + _timing.CurTime + TimeSpan.FromSeconds(rule.LockMaintsAfterGameRule);

        if (!autoLockTime.HasValue)
            return;

        while (locations.MoveNext(out var uid, out var airlock, out var accessReader, out var transform))
        {
            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == station)
            {
                if (!_accessReader.GetMainAccessReader(uid, out var mainAccessReader))
                    continue;

                var skipChecks = false;

                foreach (var accessList in mainAccessReader.Value.Comp.AccessLists)
                {
                    if (skipChecks) break;

                    foreach (var access in accessList)
                        if (rule.AirlocksAccessEmergencyList.Contains(access.Id))
                        {
                            skipChecks = true;

                            var timedAirlockStatus = EnsureComp<TimedAirlockStatusComponent>(uid);
                            timedAirlockStatus.SelfDeleteAt = autoLockTime.Value;
                            continue;
                        }
                }
            }
        }
    }

    protected override void Ended(EntityUid uid, RadiationStormComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (component.StormEntities is null)
            return;

        foreach (var entityUid in component.StormEntities)
            Del(entityUid);
    }
}
