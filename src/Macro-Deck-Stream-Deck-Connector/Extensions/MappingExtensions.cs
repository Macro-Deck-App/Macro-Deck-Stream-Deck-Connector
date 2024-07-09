using MacroDeck.StreamDeckConnector.DataTypes.Internal;
using MacroDeck.StreamDeckConnector.DataTypes.MacroDeckV2;
using SixLabors.ImageSharp;

namespace MacroDeck.StreamDeckConnector.Extensions;

public static class MappingExtensions
{
    public static ActionButton ToActionButton(this V2ActionButton v2ActionButton)
    {
        var hasBackgroundColor = Color.TryParseHex(v2ActionButton.BackgroundColorHex, out var backgroundColor);
        return new ActionButton(
            v2ActionButton.PositionY,
            v2ActionButton.PositionX,
            96,
            hasBackgroundColor ? backgroundColor : null, 
            v2ActionButton.IconBase64,
            v2ActionButton.LabelBase64);
    }
}