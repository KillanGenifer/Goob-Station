using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

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

        var effect = args[1];
        var entity = args[0];

        if (!_proto.TryIndex<RadiationEffectPrototype>(effect, out var proto))
            return;

        if (!NetEntity.TryParse(entity, out var sourceNet) || !_entityManager.TryGetEntity(sourceNet, out var source) || !_entityManager.EntityExists(source))
        {
            shell.WriteLine(Loc.GetString("shell-command-error-euid", ("arg", args[0])));
            return;
        }

        _radiation.ApplyRadiationEffect(source.Value, proto);

        shell.WriteLine(Loc.GetString("shell-command-success"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint(Loc.GetString("СУЩНОСТЬ")),
            2 => CompletionResult.FromHint(Loc.GetString("ЭФФЕКТ")),
            _ => CompletionResult.Empty
        };
    }
}
