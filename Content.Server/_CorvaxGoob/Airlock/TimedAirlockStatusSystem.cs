using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;
using Robust.Shared.Timing;

namespace Content.Server._CorvaxGoob.Airlock
{
    public sealed class TimedAirlockStatusSystem : EntitySystem
    {
        [Dependency] private readonly AirlockSystem _airlock = default!;
        [Dependency] private readonly DoorSystem _door = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TimedAirlockStatusComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<TimedAirlockStatusComponent, ComponentShutdown>(OnComponentShutdown);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = _entityManager.EntityQueryEnumerator<TimedAirlockStatusComponent>();
            while (query.MoveNext(out var uid, out var timedAirlock))
            {
                if (_timing.CurTime > timedAirlock.SelfDeleteAt)
                    RemComp<TimedAirlockStatusComponent>(uid);
            }
        }

        private void OnComponentShutdown(Entity<TimedAirlockStatusComponent> entity, ref ComponentShutdown ev)
        {
            ToggleAirlock(entity, false);
        }

        private void OnMapInit(Entity<TimedAirlockStatusComponent> entity, ref MapInitEvent ev)
        {
            ToggleAirlock(entity, true);
        }

        private void ToggleAirlock(Entity<TimedAirlockStatusComponent> entity, bool enabled)
        {
            if (!TryComp<AirlockComponent>(entity, out var airlock))
                return;

            if (!TryComp<DoorBoltComponent>(entity, out var doorBolt))
                return;


            switch (entity.Comp.AirlockStatus)
            {
                case AirlockStatus.Emergency:
                    _airlock.SetEmergencyAccess((entity.Owner, airlock), enabled);
                    break;
                case AirlockStatus.Bolts:
                    _door.SetBoltsDown((entity.Owner, doorBolt), enabled);
                    break;
                default:
                    break;
            }
        }
    }
}
