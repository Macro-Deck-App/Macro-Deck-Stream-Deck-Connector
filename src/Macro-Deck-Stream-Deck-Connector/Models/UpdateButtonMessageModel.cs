using Newtonsoft.Json;
using System.Collections.Generic;
using MacroDeck.StreamDeckConnector.DataTypes.Internal.Enums;
using MacroDeck.StreamDeckConnector.DataTypes.MacroDeckV2;

namespace MacroDeck.StreamDeckConnector.Models;

internal class UpdateButtonMessageModel : IBaseMessageModel
{
    public MessageMethod Method { get; set; } = MessageMethod.UPDATE_BUTTON;

    public List<V2ActionButton> Buttons { get; set; } = new List<V2ActionButton>();

    public string Serialize()
    {
        return JsonConvert.SerializeObject(this, Formatting.None);
    }
    public static GetButtonsMessageModel Deserialize(string json)
    {
        return ISerializableModel.Deserialize<GetButtonsMessageModel>(json);
    }
}