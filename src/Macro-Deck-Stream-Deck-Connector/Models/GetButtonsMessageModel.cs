using MacroDeck.StreamDeckConnector.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MacroDeck.StreamDeckConnector.Models;

internal class GetButtonsMessageModel : IBaseMessageModel
{
    public MessageMethod Method { get; set; } = MessageMethod.GET_BUTTONS;

    public List<ActionButtonModel> Buttons { get; set; } = new List<ActionButtonModel>();

    public string Serialize()
    {
        return JsonConvert.SerializeObject(this, Formatting.None);
    }
    public static GetButtonsMessageModel Deserialize(string json)
    {
        return ISerializableModel.Deserialize<GetButtonsMessageModel>(json);
    }
}