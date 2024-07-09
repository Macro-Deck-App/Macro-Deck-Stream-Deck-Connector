using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;
using MacroDeck.StreamDeckConnector.DataTypes.Internal.Enums;

namespace MacroDeck.StreamDeckConnector.Models;

internal interface IBaseMessageModel : ISerializableModel
{

    [JsonConverter(typeof(StringEnumConverter))]
    public MessageMethod Method { get; set; }
}