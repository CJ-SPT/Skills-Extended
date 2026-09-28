using Newtonsoft.Json;

namespace SkillsExtended.Helpers;

/// <summary>Client-side reader for the server's configuration responses.</summary>
public static class ConfigurationJson
{
    public static T Deserialize<T>(string json)
        where T : class =>
        JsonConvert.DeserializeObject<T>(
            json,
            new JsonSerializerSettings
            { // Newtonsoft otherwise populates initialized lists/dictionaries. A full server
                // snapshot must replace those defaults; omitted members still retain defaults.
                ObjectCreationHandling = ObjectCreationHandling.Replace,
            }
        );
}
