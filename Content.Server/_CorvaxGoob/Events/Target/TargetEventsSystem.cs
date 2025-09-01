using Content.Server.Humanoid;
using Content.Shared._CorvaxGoob.Events;
using Content.Shared._CorvaxGoob.Events.Actions;
using Content.Shared._CorvaxGoob.Events.Components;
using Content.Shared._CorvaxGoob.Events.HumanoidAppearance;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server._CorvaxGoob.Events;

public sealed class TargetEventsSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _seriMan = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IComponentFactory _component = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeComponentTargetEvent>(OnChangeComponent);
        SubscribeLocalEvent<ApplyEntityEffectTargetEvent>(OnEntityEffectApply);
        SubscribeLocalEvent<RemoveHumanoidAppearanceSlotTargetEvent>(OnRemoveHumanoidAppearanceSlot);
        SubscribeLocalEvent<GrantActionTargetEvent>(OnGrantActionTargetEvent);
        SubscribeLocalEvent<EditNumeralComponentDataTargetEvent>(OnEditNumeralComponentDataTargetEvent);
    }

    private void OnEditNumeralComponentDataTargetEvent(EditNumeralComponentDataTargetEvent ev)
    {
        foreach (var operation in ev.Operations)
        {
            _component.TryGetRegistration(operation.Component, out var registration, true);
            if (registration is null)
                return;

            var comp = _entity.GetComponent(ev.Target, registration.Idx);
            var field = comp.GetType().GetField(operation.Field);

            if (field is null)
                return;

            var newValue = Convert.ToSingle(field.GetValue(comp));

            switch (operation.Operation.ToLower())
            {
                case "set":
                    newValue = operation.Value;
                    break;
                case "add":
                    newValue += operation.Value;
                    break;
                case "multiply":
                    newValue *= operation.Value;
                    break;
                case "divide":
                    newValue /= operation.Value;
                    break;
                case "subtract":
                    newValue -= operation.Value;
                    break;
                default:
                    break;
            }

            field.SetValue(comp, newValue);
        }
    }

    private void OnRemoveHumanoidAppearanceSlot(RemoveHumanoidAppearanceSlotTargetEvent ev)
    {
        _humanoid.RemoveMarking(ev.Target, ev.Category, ev.Slot);
    }

    private void OnGrantActionTargetEvent(GrantActionTargetEvent ev)
    {
        if (!_prototype.TryIndex<EntityPrototype>(ev.Action, out var proto) || !proto.HasComponent<ActionComponent>())
            return;

        _actions.AddAction(ev.Target, ev.Action);
    }

    private void OnEntityEffectApply(ApplyEntityEffectTargetEvent ev)
    {
        foreach (var effect in ev.Effects)
            effect.Effect(new Shared.EntityEffects.EntityEffectBaseArgs(ev.Target, EntityManager));
    }

    private void OnChangeComponent(ChangeComponentTargetEvent ev)
    {
        RemoveComponents(ev.Target, ev.ToRemove);
        AddComponents(ev.Target, ev.ToAdd);
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
