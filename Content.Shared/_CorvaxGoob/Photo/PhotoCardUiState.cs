using Content.Shared.Eui;
using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared._CorvaxGoob.Photo;

[Serializable, NetSerializable]
public sealed class PhotoCardUiState : BoundUserInterfaceState
{
    public byte[]? ImageData { get; }

    public PhotoCardUiState(byte[]? imageData)
    {
        ImageData = imageData;
    }
}

[Serializable, NetSerializable]
public enum PhotoCardUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CaptureScreenRequestEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class CaptureScreenResponseEvent : EntityEventArgs
{
    public byte[]? Image = default;

    public CaptureScreenResponseEvent(byte[] image)
    {
        this.Image = image;
    }
}

[Serializable, NetSerializable]
public sealed class ImageEuiState : EuiStateBase
{
    public byte[]? Image;
    public Vector2? Scale;
    public bool Save;

    public ImageEuiState(byte[] image, Vector2? scale = null, bool save = false)
    {
        Image = image;
        Scale = scale;
        Save = save;
    }
}
