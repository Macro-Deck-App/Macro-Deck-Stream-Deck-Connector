using Newtonsoft.Json;

namespace MacroDeck.StreamDeckConnector.DataTypes.MacroDeckV2;

public class V2ActionButton
{
    public string BackgroundColorHex { get; set; } = "#000000";

    [JsonProperty("Position_X")]
    public int PositionX { get; set; }

    [JsonProperty("Position_Y")]
    public int PositionY { get; set; }

    public string LabelBase64 { get; set; } = string.Empty;

    public string IconBase64 { get; set; } = string.Empty;
}