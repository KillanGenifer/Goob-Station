using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles.Jobs;

using Content.Shared._CorvaxGoob.Intentions.Prototypes;
using Content.Shared._CorvaxGoob.Power.Generation.FissionGenerator;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Collections;
using System.Linq;

namespace Content.Server._CorvaxGoob.Intentions;

public sealed class IntentionsSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private GameTicker _gameTicker = default!;

    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private JobSystem _job = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (args.Player.GetMind() is not { } mindUid)
            return;

        if (!TryComp<MindComponent>(mindUid, out var mind))
            return;
    }

    /// <summary>
    /// Возрвращает список всех доступный намерений для сущности.
    /// </summary>
    public Dictionary<ProtoId<ScenarioCategoryPrototype>, List<DefinedIntention>> GetEntityAvailableIntentions(EntityUid uid, List<ICommonSession>? sessions = null)
    {
        var availableIntentions = new Dictionary<ProtoId<ScenarioCategoryPrototype>, List<DefinedIntention>>();

        var allIntentions = _prototype.EnumeratePrototypes<IntentionPrototype>();

        if (sessions == null)
            sessions = GetAvailablePlayers();

        foreach (var intention in allIntentions)
        {
            bool fail = false;

            if (!CheckRequirements(uid, intention.Entries, false))
            {
                continue;
            } // TODO: добавить флаг 

            Dictionary<string, EntityUid>? actors = null;

            if (intention.Actors is not null)
            {
                actors = new();

                if (sessions.Count == 0)
                    continue;

                foreach (var actor in intention.Actors)
                {
                    foreach (var session in sessions)
                    {
                        if (session.AttachedEntity is not { } attachedEntity)
                            continue;

                        if (actors.ContainsValue(attachedEntity))
                            continue;

                        if (attachedEntity == uid)
                            continue;

                        if (actor.Predicates is not null && !CheckRequirements(attachedEntity, actor.Predicates, actor.RequireAll))
                            continue;

                        actors.Add(actor.Key, attachedEntity);
                    }
                }
            }

            if (fail)
                continue;

            availableIntentions.GetOrNew(intention.Category).Add(new DefinedIntention(intention, actors));
        }

        return availableIntentions;
    }

    public struct DefinedIntention
    {
        public IntentionPrototype Intention { get; } // PROTOID ЛУЧШЕ 
        public Dictionary<string, EntityUid>? Actors { get; }

        public DefinedIntention(IntentionPrototype intention, Dictionary<string, EntityUid>? actors)
        {
            Intention = intention;
            Actors = actors;
        }
    }

    public List<ICommonSession> GetAvailablePlayers()
    {
        var players = _player.Sessions
            .Where(x => _gameTicker.PlayerGameStatuses.TryGetValue(x.UserId, out var status) && status == PlayerGameStatus.JoinedGame)
            .ToList();

        _random.Shuffle(players);

        return players;
    }

    public bool CheckRequirements(EntityUid uid, List<PredicateDefinition> requirements, bool requireAll)
    {
        var antagRolesList = new List<ProtoId<AntagPrototype>>();
        var objectivesList = new List<ProtoId<EntityPrototype>>();
        var components = new List<string>();

        if (!_mind.TryGetMind(uid, out var mindUid, out var mind))
            return false;

        TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearance);

        _job.MindTryGetJobId(mindUid, out var jobProto);

        DepartmentPrototype? departmentProto = null;
        if (jobProto is not null)
            _job.TryGetDepartment(jobProto, out departmentProto);

        foreach (var role in mind.MindRoleContainer.ContainedEntities)
            if (TryComp<MindRoleComponent>(role, out var mindRole) && mindRole.Antag && mindRole.AntagPrototype is not null)
                antagRolesList.Add(mindRole.AntagPrototype.Value);

        foreach (var objective in mind.Objectives)
        {
            var objectiveMeta = MetaData(objective);
            if (objectiveMeta is not null && objectiveMeta.EntityPrototype is not null)
                objectivesList.Add(objectiveMeta.EntityPrototype.ID);
        }

        IEnumerator componentsEnumerator = EntityManager.GetComponents(uid).GetEnumerator();
        while (componentsEnumerator.MoveNext())
        {
            if (componentsEnumerator.Current is not null)
                components.Add(componentsEnumerator.Current.GetType().Name);
        }

        var success = false;

        if (requirements.Count == 0)
            success = true;
        else
            foreach (var requirement in requirements)
                if (!CheckRequirement(requirement, jobProto, departmentProto?.ID, humanoidAppearance, antagRolesList, objectivesList, components))
                {
                    if (requireAll)
                        return false;
                }
                else
                    success = true;

        return success;
    }

    public bool CheckRequirement(PredicateDefinition requirement, ProtoId<JobPrototype>? jobProto, ProtoId<DepartmentPrototype>? departmentProto, HumanoidAppearanceComponent? humanoidAppearance, List<ProtoId<AntagPrototype>> antagRolesList, List<ProtoId<EntityPrototype>> objectivesList, List<string> components)
    {
        switch (requirement.Field)
        {
            case PredicateDefinitionField.Job:
                if (jobProto is null || !CompareString(requirement.Operator, requirement.Key, jobProto))
                    return false;
                break;

            case PredicateDefinitionField.Department:
                if (departmentProto is null || !CompareString(requirement.Operator, requirement.Key, departmentProto))
                    return false;
                break;

            case PredicateDefinitionField.Age:
                if (humanoidAppearance?.Age is null || !ValidAndCompareNumber(requirement.Operator, requirement.Key, humanoidAppearance.Age))
                    return false;
                break;

            case PredicateDefinitionField.Species:
                if (humanoidAppearance?.Species is null || !CompareString(requirement.Operator, requirement.Key, humanoidAppearance.Species))
                    return false;
                break;

            case PredicateDefinitionField.Sex:
                if (humanoidAppearance?.Sex is null || !CompareString(requirement.Operator, requirement.Key, humanoidAppearance.Sex.ToString()))
                    return false;
                break;

            case PredicateDefinitionField.AntagRole:
                if (antagRolesList.Count == 0 || !CompareList<ProtoId<AntagPrototype>>(requirement.Operator, antagRolesList, requirement.Key))
                    return false;
                break;

            case PredicateDefinitionField.AntagObjective:
                if (objectivesList.Count == 0 || !CompareList<ProtoId<EntityPrototype>>(requirement.Operator, objectivesList, requirement.Key))
                    return false;
                break;

            case PredicateDefinitionField.Component:
                if (components.Count == 0 || !CompareList<string>(requirement.Operator, components, requirement.Key))
                    return false;
                break;
        }

        return true;
    }

    private bool CompareString(PredicateDefinitionOperator defOperator, string key, string value)
    {
        switch (defOperator)
        {
            case PredicateDefinitionOperator.Equals:
                return key == value;
            case PredicateDefinitionOperator.NotEquals:
                return key != value;
            case PredicateDefinitionOperator.Contains:
                return key.Contains(value);
            case PredicateDefinitionOperator.NotContains:
                return !key.Contains(value);
        }

        return false;
    }

    private bool CompareList<T>(PredicateDefinitionOperator defOperator, List<T> key, T value)
    {
        switch (defOperator)
        {
            case PredicateDefinitionOperator.Contains:
                return key.Contains(value);
            case PredicateDefinitionOperator.NotContains:
                return !key.Contains(value);
        }

        return false;
    }

    private bool ValidAndCompareNumber(PredicateDefinitionOperator defOperator, string key, float value)
    {
        if (!float.TryParse(key, out var floatKey))
            return false;

        switch (defOperator)
        {
            case PredicateDefinitionOperator.Equals:
                return floatKey == value;
            case PredicateDefinitionOperator.NotEquals:
                return floatKey != value;
            case PredicateDefinitionOperator.GreaterThan:
                return floatKey > value;
            case PredicateDefinitionOperator.GraterThanOrEquals:
                return floatKey >= value;
            case PredicateDefinitionOperator.LessThan:
                return floatKey < value;
            case PredicateDefinitionOperator.LessThanOrEquals:
                return floatKey <= value;
        }

        return false;
    }

    private bool ValidAndCompareBool(PredicateDefinitionOperator defOperator, string key, string value)
    {
        if (!bool.TryParse(key, out var boolKey))
            return false;

        if (!bool.TryParse(value, out var boolValue))
            return false;

        switch (defOperator)
        {
            case PredicateDefinitionOperator.Equals:
                return boolKey == boolValue;
            case PredicateDefinitionOperator.NotEquals:
                return boolKey != boolValue;
        }

        return false;
    }
}
