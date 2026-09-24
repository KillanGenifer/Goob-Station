using Content.Server.Botany;
using Content.Server.CrewManifest;
using Content.Server.GameTicking.Rules;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Server.Mind.Toolshed;
using Content.Server.Station.Systems;
using Content.Shared._CorvaxGoob.Intentions.Prototypes;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;
using static Content.Server._CorvaxGoob.Intentions.IntentionsSystem;

namespace Content.Server._CorvaxGoob.Intentions;

public sealed partial class IntentionsWaveRuleSystem : GameRuleSystem<IntentionsWaveRuleComponent>
{
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IntentionsSystem _intentions = default!;

    protected override void Started(EntityUid uid, IntentionsWaveRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var sessions = _intentions.GetAvailablePlayers();

        if (sessions.Count == 0)
            return;

        foreach (var session in sessions)
        {
            if (session.AttachedEntity is not { } playerUid)
                return;

            if (!ValidEntity(playerUid))
                continue;

            if (session.GetMind() is not { } mindUid)
                continue;

            if (!TryComp<MindComponent>(mindUid, out var mind))
                continue;

            var availableIntentions = _intentions.GetEntityAvailableIntentions(playerUid);
            if (availableIntentions.Count == 0)
                continue;

            var pickedIntentions = new List<DefinedIntention>();

            if (component.IntentionsPerCategory is not null)
            {
                if (component.IntentionsPerCategory.Count == 0)
                    continue;

                foreach (var category in component.IntentionsPerCategory)
                {
                    var pickedCount = _random.Next(category.Value.X, category.Value.Y);
                    if (pickedCount == 0)
                        continue;

                    if (!availableIntentions.TryGetValue(category.Key, out var intentions))
                        return;

                    for (var i = 0; i < pickedCount; i++)
                    {
                        var pickedIntention = PickRandom(intentions);
                        pickedIntentions.Add(pickedIntention);

                        intentions.Remove(pickedIntention);
                    }
                }
            }
            else
            {
                int pickedIntentionsCount = _random.Next(component.MinPickIntentions, component.MaxPickIntentions);
                if (pickedIntentionsCount == 0)
                    continue;

                var allAvailableIntentions = new List<DefinedIntention>();

                foreach (var categories in availableIntentions)
                    foreach (var intention in categories.Value)
                        allAvailableIntentions.Add(intention);

                for (var i = 0; i < pickedIntentionsCount; i++)
                {
                    var pickedIntention = PickRandom(allAvailableIntentions);
                    pickedIntentions.Add(pickedIntention);

                    allAvailableIntentions.Remove(pickedIntention);
                }

            }

            foreach (var intention in pickedIntentions)
            {
                var intentionData = new IntentionData(intention.Intention.Name, intention.Intention.Description, intention.Intention);

                mind.Intentions.Add(intentionData);

                if (intention.Actors is not null)
                    foreach (var actor in intention.Intention.Actors)
                    {
                        if (!intention.Actors.TryGetValue(actor.Key, out var actorUid) || !_mind.TryGetMind(actorUid, out var actorMindUid, out var actorMindComp))
                            continue;

                        var actorIntentionData = new IntentionData(actor.Name, actor.Description, intention.Intention);

                        actorMindComp.Intentions.Add(actorIntentionData);
                    }
            }

            DefinedIntention PickRandom(List<DefinedIntention> intentions)
            {
                var totalWeight = intentions.Sum(x => x.Intention.Weight);
                double randomValue = Random.Shared.NextDouble() * totalWeight;

                foreach (var intention in intentions)
                {
                    randomValue -= intention.Intention.Weight;
                    if (randomValue <= 0)
                        return intention;
                }
                return intentions.Last();
            }
        }
    }

    private bool ValidEntity(EntityUid? uid)
    {
        if (uid is null)
            return false;

        if (HasComp<GhostComponent>(uid))
            return false;

        return true;
    }
}
