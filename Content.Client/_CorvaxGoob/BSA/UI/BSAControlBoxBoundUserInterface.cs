using Content.Shared.Radio.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._CorvaxGoob.BSA;

[UsedImplicitly]
public sealed class BSAControlBoxBoundUserInterface : BoundUserInterface
{
    //[ViewVariables]
    //private BSAControlBoxControlWindow? _menu;

    [ViewVariables]
    private BSAControlBoxBuildWindow? _menu2;

    public BSAControlBoxBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        //_menu = this.CreateWindow<BSAControlBoxControlWindow>();
        _menu2 = this.CreateWindow<BSAControlBoxBuildWindow>();
    }

    public void Update(Entity<IntercomComponent> ent)
    {

    }
}
