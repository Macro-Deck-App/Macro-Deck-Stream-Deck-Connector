using MacroDeck.StreamDeckConnector.Enums;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MacroDeck.StreamDeckConnector.Models
{
    internal interface IBaseMessageModel : ISerializableModel
    {

        [JsonConverter(typeof(StringEnumConverter))]
        public MessageMethod Method { get; set; }
    }
}
