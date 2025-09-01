using Content.Client.Markers;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Shared.Console;

namespace Content.Server._CorvaxGoob.Commands;

public sealed class ShowAreasCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    private bool _enabled = false;
    private int _eyeMask = 1;

    public override string Command => "showareas";
    public override string Description => Loc.GetString("cmd-apply-showareas-effect-desc");
    public override string Help => Loc.GetString("cmd-apply-showareas-effect-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_playerManager.LocalEntity is null)
            return;

        if (!_entityManager.TryGetComponent<EyeComponent>(_playerManager.LocalEntity, out var eye))
            return;

        _enabled = !_enabled;

        if (_enabled)
            _consoleHost.ExecuteCommand($"vvwrite /entity/{_entityManager.GetNetEntity(_playerManager.LocalEntity)}/Eye/VisibilityMask 5");
        else
            _consoleHost.ExecuteCommand($"vvwrite /entity/{_entityManager.GetNetEntity(_playerManager.LocalEntity)}/Eye/VisibilityMask {_eyeMask}");

        _entitySystemManager.GetEntitySystem<MarkerSystem>().MarkersVisible = _enabled;

        _eyeMask = eye.VisibilityMask;
    }
}
