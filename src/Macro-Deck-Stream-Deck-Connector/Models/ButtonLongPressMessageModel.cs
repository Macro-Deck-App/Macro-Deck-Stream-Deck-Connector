using MacroDeck.StreamDeckConnector.Enums;
using Newtonsoft.Json;

namespace MacroDeck.StreamDeckConnector.Models;

internal class ButtonLongPressMessageModel : IBaseMessageModel
{
    public MessageMethod Method { get; set; } = MessageMethod.BUTTON_LONG_PRESS;

    [JsonProperty("Message")]
    public string Id { get; set; }

    public string Serialize()
    {
        return JsonConvert.SerializeObject(this, Formatting.None);
    }
    public static ButtonLongPressMessageModel Deserialize(string json)
    {
        return ISerializableModel.Deserialize<ButtonLongPressMessageModel>(json);
    }
}