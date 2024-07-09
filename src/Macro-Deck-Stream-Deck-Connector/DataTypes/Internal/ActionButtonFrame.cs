using OpenMacroBoard.SDK;

namespace MacroDeck.StreamDeckConnector.DataTypes.Internal;

public class ActionButtonFrame
{
    public int Delay { get; set; }

    public KeyBitmap KeyBitmap { get; set; }

    public ActionButtonFrame(int delay, KeyBitmap keyBitmap)
    {
        Delay = delay;
        KeyBitmap = keyBitmap;
    }
}