using Content.Server.Administration;
using Content.Shared._CorvaxGoob.Intentions.Prototypes;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CorvaxGoob.Intentions.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class IntentionCheckRequirementCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IntentionsSystem _intentions = default!;

    public override string Command => "intentioncheckrequirement";

    public override string Description => Loc.GetString("cmd-intentioncheckrequirement-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!int.TryParse(args[0], out var entInt))
        {
            shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!EntityManager.TryGetEntity(new NetEntity(entInt), out var eUid))
        {
            shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
            return;
        }

        if (!_proto.TryIndex<IntentionPrototype>(args[1], out var intention))
        {
            shell.WriteLine(Loc.GetString("shell-argument-must-be-prototype", ("index", args[1]), ("prototypeName", nameof(IntentionPrototype))));
            return;
        }

        var isValidSelfRequirements = _intentions.CheckRequirements(eUid.Value, intention.Entries, intention.RequireAll);

        shell.WriteLine(Loc.GetString("cmd-intentioncheckrequirement-owner-checks", ("valid", isValidSelfRequirements)));

        var players = _intentions.GetAvailablePlayers();

        _random.Shuffle(players);

        var pickedActors = new List<EntityUid>();
        if (intention.Actors is not null)
            foreach (var actor in intention.Actors)
            {
                var isFound = false;

                foreach (var session in players)
                {
                    if (session.AttachedEntity is not { } attachedEntity)
                        continue;

                    if (pickedActors.Contains(attachedEntity))
                        continue;

                    if (attachedEntity == eUid)
                        continue;

                    if (actor.Predicates is not null && !_intentions.CheckRequirements(eUid.Value, intention.Entries, intention.RequireAll))
                        continue;

                    pickedActors.Add(attachedEntity);

                    isFound = true;

                    shell.WriteLine(Loc.GetString("cmd-intentioncheckrequirement-actor-checks",
                        ("actor", actor.Key),
                        ("name", EntityManager.GetComponent<MetaDataComponent>(attachedEntity).EntityName)));

                    break;
                }

                if (!isFound)
                    shell.WriteLine(Loc.GetString("cmd-intentioncheckrequirement-actor-not-found", ("actor", actor.Key)));
            }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("shell-argument-uid"));

        if (args.Length == 2)
            return CompletionResult.FromOptions(CompletionHelper.PrototypeIDs<IntentionPrototype>());

        return CompletionResult.Empty;
    }
}
