using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;

namespace Content.Server._CorvaxGoob.BSA;
public sealed partial class BSASystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BSAControlPanelComponent, InteractUsingEvent>(OnInteractUsing);

        _partsOffsets = new()
        {
            [BSAPartType.FuelChamber] = new Vector2i(0, -1),
            [BSAPartType.PowerBox] = new Vector2i(0, -1),
            [BSAPartType.Emitter] = new Vector2i(0, 1),
        };
    }
}
