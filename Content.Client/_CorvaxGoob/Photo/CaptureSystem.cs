using Content.Client.UserInterface.Controls;
using Content.Shared._CorvaxGoob.Photo;
using Robust.Client.UserInterface;
using SixLabors.ImageSharp;
using System.IO;

namespace Content.Client._CorvaxGoob.Photo;
public sealed class CaptureSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CaptureScreenRequestEvent>(RequestCaptureScreen);
    }

    private async void RequestCaptureScreen(CaptureScreenRequestEvent ev)
    {
        if (_uiManager.ActiveScreen == null || !_uiManager.ActiveScreen!.TryGetWidget<MainViewport>(out var mainViewport))
            return;

        mainViewport.Viewport.Screenshot(image =>
        {
            using var data = new MemoryStream();

            image.SaveAsPng(data);
            var bytes = data.ToArray();

            image.Dispose();

            RaiseNetworkEvent(new CaptureScreenResponseEvent(bytes));
        });
    }
}
