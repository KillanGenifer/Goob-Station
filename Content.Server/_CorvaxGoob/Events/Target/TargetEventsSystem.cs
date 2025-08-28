using Content.Shared._CorvaxGoob.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server._CorvaxGoob.Events;

public sealed class TargetEventsSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _seriMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeComponentTargetEvent>(OnChangeComponent);
        SubscribeLocalEvent<ApplyEntityEffectTargetEvent>(OnEnttyEffectApply);
    }

    private void OnEnttyEffectApply(ApplyEntityEffectTargetEvent ev)
    {
        foreach (var effect in ev.Effects)
            effect.Effect(new Shared.EntityEffects.EntityEffectBaseArgs(ev.Target, EntityManager));
    }

    private void OnChangeComponent(ChangeComponentTargetEvent ev)
    {
        AddComponents(ev.Target, ev.ToAdd);
        RemoveComponents(ev.Target, ev.ToRemove);
    }

    private void AddComponents(EntityUid target, ComponentRegistry comps)
    {
        foreach (var (name, data) in comps)
        {
            if (HasComp(target, data.Component.GetType()))
                continue;

            var component = (Component) Factory.GetComponent(name);
            var temp = (object) component;
            _seriMan.CopyTo(data.Component, ref temp);
            EntityManager.AddComponent(target, (Component) temp!);
        }
    }

    private void RemoveComponents(EntityUid target, HashSet<string> comps)
    {
        foreach (var toRemove in comps)
        {
            if (Factory.TryGetRegistration(toRemove, out var registration))
                RemComp(target, registration.Type);
        }
    }
}
