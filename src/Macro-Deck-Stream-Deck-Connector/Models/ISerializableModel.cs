using Newtonsoft.Json;

namespace MacroDeck.StreamDeckConnector.Models;

internal interface ISerializableModel
{
    public string Serialize();
    protected static T Deserialize<T>(string configuration) where T : ISerializableModel, new() =>
        !string.IsNullOrWhiteSpace(configuration) ? JsonConvert.DeserializeObject<T>(configuration) : new T();
}