using MacroDeck.StreamDeckConnector.Enums;
using Newtonsoft.Json;

namespace MacroDeck.StreamDeckConnector.Models;

internal class BasicMessageModel : IBaseMessageModel
{
    public MessageMethod Method { get; set; }


    public string Serialize()
    {
        return JsonConvert.SerializeObject(this, Formatting.None);
    }
    public static ConnectedMessageModel Deserialize(string json)
    {
        return ISerializableModel.Deserialize<ConnectedMessageModel>(json);
    }
}