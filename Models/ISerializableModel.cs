using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MacroDeck.StreamDeckConnector.Models
{
    internal interface ISerializableModel
    {
        public string Serialize();
        protected static T Deserialize<T>(string configuration) where T : ISerializableModel, new() =>
            !string.IsNullOrWhiteSpace(configuration) ? JsonConvert.DeserializeObject<T>(configuration) : new T();
    }
}
