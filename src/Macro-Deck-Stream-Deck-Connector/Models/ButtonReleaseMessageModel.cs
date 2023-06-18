using MacroDeck.StreamDeckConnector.Enums;
using Newtonsoft.Json;

namespace MacroDeck.StreamDeckConnector.Models;

internal class ButtonReleaseMessageModel : IBaseMessageModel
{
    public MessageMethod Method { get; set; } = MessageMethod.BUTTON_RELEASE;

    [JsonProperty("Message")]
    public string Id { get; set; }

    public string Serialize()
    {
        return JsonConvert.SerializeObject(this, Formatting.None);
    }
    public static ButtonReleaseMessageModel Deserialize(string json)
    {
        return ISerializableModel.Deserialize<ButtonReleaseMessageModel>(json);
    }
}