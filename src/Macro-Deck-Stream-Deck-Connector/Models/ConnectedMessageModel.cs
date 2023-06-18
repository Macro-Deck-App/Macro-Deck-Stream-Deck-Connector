using MacroDeck.StreamDeckConnector.Enums;
using Newtonsoft.Json;

namespace MacroDeck.StreamDeckConnector.Models;

internal class ConnectedMessageModel : IBaseMessageModel
{
    public MessageMethod Method { get; set; } = MessageMethod.CONNECTED;

    [JsonProperty("Client-Id")]
    public string ClientId { get; set; } = "STRDCK";

    [JsonProperty("API")]
    public string ApiVersion { get; } = "20";

    [JsonProperty("Device-Type")]
    public string DeviceType { get; } = "Web";
        


    public string Serialize()
    {
        return JsonConvert.SerializeObject(this, Formatting.None);
    }
    public static ConnectedMessageModel Deserialize(string json)
    {
        return ISerializableModel.Deserialize<ConnectedMessageModel>(json);
    }
}