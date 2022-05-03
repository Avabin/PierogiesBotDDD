using System.Text;
using Newtonsoft.Json;

namespace Shared.Core.Events;

public static class EventExtensions
{
    private static JsonSerializerSettings _settings = new JsonSerializerSettings
    {
        TypeNameHandling               = TypeNameHandling.Objects,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
    };

    public static byte[] ToMessageBody(this IEvent @event, JsonSerializerSettings? settings = null)
    {
        // serialize events to byte array
        var json = JsonConvert.SerializeObject(@event, Formatting.None, settings ?? _settings);
        return Encoding.UTF8.GetBytes(json);
    }

    public static T ToEvent<T>(this byte[] body, JsonSerializerSettings? settings = null)
    {
        // deserialize events from byte array
        var json = Encoding.UTF8.GetString(body);
        return JsonConvert.DeserializeObject<T>(json, settings ?? _settings) ??
               throw new InvalidOperationException($"Unable to deserialize event {typeof(T).Name}: {json}");
    }
}