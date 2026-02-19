using Content.Client.Eui;
using Content.Shared._CorvaxGoob.Photo;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._CorvaxGoob.Photo.UI;

[UsedImplicitly]
public sealed class ImageEui : BaseEui
{
    private readonly ImageUi _window;

    public ImageEui()
    {
        _window = new ImageUi();
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not ImageEuiState cast)
            return;

        if (cast.Image is not null)
            _window.SetImage(cast.Image);

        if (cast.Scale is not null)
            _window.SetScale(cast.Scale.Value);

        if (cast.Save)
            _window.SaveImage(DateTime.Now.ToString("yyyy-M-dd_HH.mm.ss"));
    }
}
