using MacroDeck.StreamDeckConnector.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroDeck.StreamDeckConnector.Models
{
    internal class ButtonLongPressReleaseMessageModel : IBaseMessageModel
    {
        public MessageMethod Method { get; set; } = MessageMethod.BUTTON_LONG_PRESS_RELEASE;

        [JsonProperty("Message")]
        public string Id { get; set; }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }
        public static ButtonLongPressReleaseMessageModel Deserialize(string json)
        {
            return ISerializableModel.Deserialize<ButtonLongPressReleaseMessageModel>(json);
        }
    }
}
