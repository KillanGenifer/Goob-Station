using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server._CorvaxGoob.Radiation;

[AdminCommand(AdminFlags.Fun)]
public sealed class ApplyRadiationEffectCommand : LocalizedEntityCommands
{
    [Dependency] private readonly RadiationSystem _radiation = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override string Command => "applyradiationeffect";
    public override string Description => Loc.GetString("cmd-apply-radiation-effect-desc");
    public override string Help => Loc.GetString("cmd-apply-radiation-effect-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        switch (args.Length)
        {
            case 0:
                shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
                return;
            case > 2:
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
        }

        if (!_proto.TryIndex<RadiationEffectPrototype>(args[1], out var proto))
            return;

        if (!NetEntity.TryParse(args[0], out var sourceNet) || !_entityManager.TryGetEntity(sourceNet, out var source) || !_entityManager.EntityExists(source))
        {
            shell.WriteLine(Loc.GetString("shell-command-error-euid", ("arg", args[0])));
            return;
        }

        _radiation.ApplyRadiationEffect(source.Value, proto);

        shell.WriteLine(Loc.GetString("shell-command-success"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("EntityUid"));
        else if (args.Length == 2)
        {
            var effects = _proto.EnumeratePrototypes<RadiationEffectPrototype>().OrderBy(p => p.ID);
            var options = new List<string>();
            foreach (var effect in effects)
            {
                options.Add(effect.ID);
            }

            return CompletionResult.FromHintOptions(options, "<id>");
        }

        return CompletionResult.Empty;
    }
}
