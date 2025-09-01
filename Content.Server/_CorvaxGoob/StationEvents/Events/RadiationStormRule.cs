using Content.Server.Chat.Systems;
using Content.Server.Radiation.Components;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Server.Weather;
using Content.Shared.Damage;
using Content.Shared.GameTicking.Components;
using Content.Shared.Maps;
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
    protected override void Added(EntityUid uid, RadiationStormComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        component.TimeBeforeAlert = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(component.MinTimeBeforeAlert, component.MaxTimeBeforeAlert));
    }

    protected override void ActiveTick(EntityUid uid, RadiationStormComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

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

        if (component.DamageTime < _timing.CurTime)
        {
            component.DamageTime = _timing.CurTime + TimeSpan.FromSeconds(component.DamageDelay);

            var query = _entityManager.EntityQueryEnumerator<RadiationReceiverComponent>();
            while (query.MoveNext(out var receiverUid, out var receiverComponent)) if (Transform(receiverUid).GridUid == null)
                    _damageable.TryChangeDamage(receiverUid, component.Damage);
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
                component.StormEntities.Add(Spawn("Radstorm", tileCenter));
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
