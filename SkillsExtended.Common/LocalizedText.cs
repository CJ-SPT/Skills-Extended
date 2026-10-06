using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SkillsExtended;

/// <summary>Server locale keys resolved at the point of display, with bundled English for startup/offline use.</summary>
public static class LocalizedText
{
    public const string Prefix = "SkillsExtended.";
    private const string MessagePrefix = "@SkillsExtended:";
    private static readonly Lazy<Dictionary<string, string>> English = new(LoadEnglish);
    public static Func<string, string> Resolver { private get; set; }

    public static string Get(string key, params object[] arguments)
    {
        if (key == null)
            return null;
        string template = null;
        try
        {
            template = Resolver?.Invoke(key);
        }
        catch (Exception)
        { /* Game locales are not available during early startup. */
        }
        var fallback = English.Value.TryGetValue(key, out var english) ? english : key;
        if (string.IsNullOrEmpty(template) || template == key)
            template = fallback;
        if (arguments.Length == 0)
            return template;
        var values = new object[arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
            values[i] = arguments[i] is string text ? Resolve(text) : arguments[i];
        try
        {
            return string.Format(CultureInfo.CurrentCulture, template, values);
        }
        catch (FormatException)
        {
            return string.Format(CultureInfo.CurrentCulture, fallback, values);
        }
    }

    /// <summary>Keep authority messages language-neutral until the receiving client displays them.</summary>
    public static string Message(string key, params object[] arguments)
    {
        if (arguments.Length == 0)
            return key;
        using var stream = new MemoryStream();
        MessageSerializer()
            .WriteObject(stream, new DeferredMessage { Key = key, Arguments = arguments });
        return MessagePrefix + Encoding.UTF8.GetString(stream.ToArray());
    }

    public static string Resolve(string text)
    {
        if (text == null)
            return null;
        if (text.StartsWith(MessagePrefix, StringComparison.Ordinal))
        {
            try
            {
                using var stream = new MemoryStream(
                    Encoding.UTF8.GetBytes(text.Substring(MessagePrefix.Length))
                );
                var message = (DeferredMessage)MessageSerializer().ReadObject(stream);
                return message?.Key == null
                    ? text
                    : Get(message.Key, message.Arguments ?? Array.Empty<object>());
            }
            catch (SerializationException)
            {
                return text;
            }
        }
        return text.StartsWith(Prefix, StringComparison.Ordinal) ? Get(text) : text;
    }

    private static DataContractJsonSerializer MessageSerializer() => new(typeof(DeferredMessage));

    private static Dictionary<string, string> LoadEnglish()
    {
        using var stream = typeof(LocalizedText).Assembly.GetManifestResourceStream(
            "SkillsExtended.Locales.en.json"
        );
        return (Dictionary<string, string>)
            new DataContractJsonSerializer(
                typeof(Dictionary<string, string>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true }
            ).ReadObject(stream);
    }

    [DataContract]
    private sealed class DeferredMessage
    {
        [DataMember]
        public string Key;

        [DataMember]
        public object[] Arguments;
    }
}
