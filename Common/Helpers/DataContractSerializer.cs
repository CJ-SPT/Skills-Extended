using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;

namespace SkillsExtended.Helpers;

public static class DataContractSerializer
{
    public static string Serialize<T>(T obj) where T : notnull
    {
        var serializer = new DataContractJsonSerializer(typeof(T));
        using var stream = new MemoryStream();
        
        serializer.WriteObject(stream, obj);
        
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static T Deserialize<T>(string data) where T : class
    {
        var serializer = new DataContractJsonSerializer(typeof(T));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
        
        return (T)serializer.ReadObject(stream);
    }
}