using Content.Server.EUI;
using Content.Shared._CorvaxGoob.Photo;
using System.Numerics;

namespace Content.Server._CorvaxGoob.Photo;

public sealed class ImageEui : BaseEui
{
    private byte[] _image;
    private Vector2? _scale;
    private bool _save;
    public ImageEui(byte[] image, Vector2? scale = null, bool save = false)
    {
        _image = image;
        _scale = scale;
        _save = save;

    }
    public override ImageEuiState GetNewState()
    {
        return new ImageEuiState(_image, _scale, _save);
    }
}
