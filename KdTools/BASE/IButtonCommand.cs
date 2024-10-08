using System.Drawing;

namespace KdTools.BASE;

public interface IButtonCommand
{
    string ButtonText { get; }
    string Title { get; }
    string Tooltip { get; }
    Bitmap Logo { get; }
}
public interface IButtonWithNativeImage : IButtonCommand
{
    string NativeItemId { get; }
}
