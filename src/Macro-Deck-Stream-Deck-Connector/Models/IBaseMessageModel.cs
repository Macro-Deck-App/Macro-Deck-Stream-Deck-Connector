using MacroDeck.StreamDeckConnector.Enums;
using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;

namespace MacroDeck.StreamDeckConnector.Models;

internal interface IBaseMessageModel : ISerializableModel
{

    [JsonConverter(typeof(StringEnumConverter))]
    public MessageMethod Method { get; set; }
}