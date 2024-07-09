using Newtonsoft.Json;
using System.Collections.Generic;
using MacroDeck.StreamDeckConnector.DataTypes.Internal.Enums;
using MacroDeck.StreamDeckConnector.DataTypes.MacroDeckV2;

namespace MacroDeck.StreamDeckConnector.Models;

internal class GetButtonsMessageModel : IBaseMessageModel
{
    public MessageMethod Method { get; set; } = MessageMethod.GET_BUTTONS;

    public List<V2ActionButton> Buttons { get; set; } = new();

    public string Serialize()
    {
        return JsonConvert.SerializeObject(this, Formatting.None);
    }
    public static GetButtonsMessageModel Deserialize(string json)
    {
        return ISerializableModel.Deserialize<GetButtonsMessageModel>(json);
    }
}