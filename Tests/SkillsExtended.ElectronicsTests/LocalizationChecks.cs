using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SkillsExtended;
using SkillsExtended.Config.Skills;
using SkillsExtended.Hacking;

internal static class LocalizationChecks
{
    public static void Verify()
    {
        var checks = 0;
        void Check(bool value, string message)
        {
            checks++;
            if (!value)
                throw new Exception("Localization: " + message);
        }
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            const string pick = "SkillsExtended.WorldInteractionUtils.PickLock";
            const string progress = "SkillsExtended.SignalsModel.Progress";
            LocalizedText.Resolver = null;
            Check(LocalizedText.Get(pick) == "Pick lock", "English works before game locales load");
            LocalizedText.Resolver = key => key;
            Check(
                LocalizedText.Get(pick) == "Pick lock",
                "missing game key falls back to bundled English"
            );
            LocalizedText.Resolver = _ => throw new InvalidOperationException("locales not loaded");
            Check(
                LocalizedText.Get(pick) == "Pick lock",
                "unavailable game lookup cannot break a notification"
            );
            LocalizedText.Resolver = key => key == pick ? "Замок" : key;
            Check(LocalizedText.Get(pick) == "Замок", "Unicode game translation is used");
            LocalizedText.Resolver = key => key == pick ? "Schloss knacken" : key;
            Check(
                LocalizedText.Get(pick) == "Schloss knacken",
                "subsequent lookups follow a changed game language"
            );
            Check(
                LocalizedText.Resolve("Custom administrator name") == "Custom administrator name",
                "authored names remain intact"
            );
            Check(LocalizedText.Resolve(null) == null, "absent error stays absent");
            Check(
                LocalizedText.Get("SkillsExtended.Unknown.Key") == "SkillsExtended.Unknown.Key",
                "unknown keys remain diagnosable"
            );

            // The host's language must never be baked into a replicated state or error.
            var lookups = 0;
            LocalizedText.Resolver = key =>
            {
                lookups++;
                return "HOST LANGUAGE";
            };
            var wire = LocalizedText.Message(
                progress,
                "SkillsExtended.SignalsModel.Recording",
                1.25d
            );
            Check(
                lookups == 0 && !wire.Contains("HOST LANGUAGE"),
                "authority serialization does not resolve language"
            );
            LocalizedText.Resolver = key =>
                key switch
                {
                    progress => "{1:0.0}s — {0}",
                    "SkillsExtended.SignalsModel.Recording" => "Aufnahme",
                    _ => key,
                };
            Check(
                LocalizedText.Resolve(wire) == "1.3s — Aufnahme",
                "receiver translates nested keys and can reorder numeric arguments"
            );
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Check(
                LocalizedText.Resolve(wire) == "1,3s — Aufnahme",
                "deferred numbers retain their type for local formatting"
            );
            LocalizedText.Resolver = key => key == progress ? "bad {9}" : key;
            Check(
                LocalizedText.Resolve(wire) == "Recording: 1,3s",
                "bad translator placeholders use English safely"
            );
            LocalizedText.Resolver = key => key == progress ? "bad {" : key;
            Check(
                LocalizedText.Resolve(wire) == "Recording: 1,3s",
                "malformed translator braces use English safely"
            );
            LocalizedText.Resolver = null;
            Check(
                LocalizedText.Resolve(
                    LocalizedText.Message(progress, "SkillsExtended.SignalsModel.Pairing", .5f)
                ) == "Pairing: 0,5s",
                "float authority arguments round trip"
            );
            Check(
                LocalizedText.Resolve("@SkillsExtended:not-json") == "@SkillsExtended:not-json",
                "malformed remote messages do not crash display"
            );
            Check(
                LocalizedText.Resolve("@SkillsExtended:null") == "@SkillsExtended:null",
                "null remote payload does not crash display"
            );
            var authority = new HackingAuthority(new HackingData(), 42);
            var reply = authority.Process(
                new HackRequest
                {
                    Raid = "stale",
                    Actor = "player",
                    Operation = "start",
                },
                0,
                0,
                null
            );
            Check(reply.Error.StartsWith(LocalizedText.Prefix), "authority errors carry keys");
            Check(
                !LocalizedText.Resolve(reply.Error).StartsWith(LocalizedText.Prefix),
                "authority error has a usable English fallback"
            );

            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "Skills Extended.sln")))
                root = root.Parent;
            if (root == null)
                throw new Exception("Cannot locate repository for locale validation.");
            var localeDir = Path.Combine(
                root.FullName,
                "Server/SkillsExtended.Server/Resources/Locales"
            );
            var english = ReadLocale(Path.Combine(localeDir, "en.json"));
            foreach (var pair in english.Where(pair => pair.Key.StartsWith(LocalizedText.Prefix)))
            {
                var format = CompositeFormat.Parse(pair.Value);
                Check(!string.IsNullOrWhiteSpace(pair.Value), "nonempty English entry " + pair.Key);
                Check(
                    LocalizedText.Get(pair.Key) == pair.Value,
                    "embedded fallback matches the server catalog: " + pair.Key
                );
                if (format.MinimumArgumentCount > 0)
                    Check(
                        !string.IsNullOrEmpty(
                            LocalizedText.Get(
                                pair.Key,
                                Enumerable.Repeat<object>(1, format.MinimumArgumentCount).ToArray()
                            )
                        ),
                        "format is usable: " + pair.Key
                    );
            }
            foreach (var file in Directory.GetFiles(localeDir, "*.json"))
            foreach (
                var pair in ReadLocale(file)
                    .Where(pair => pair.Key.StartsWith(LocalizedText.Prefix))
            )
            {
                Check(
                    english.ContainsKey(pair.Key),
                    "translated key exists in English: " + pair.Key
                );
                var expected = Regex
                    .Matches(english[pair.Key], @"\{(\d+)(?:[,}:])")
                    .Select(m => m.Groups[1].Value)
                    .Order()
                    .ToArray();
                var actual = Regex
                    .Matches(pair.Value, @"\{(\d+)(?:[,}:])")
                    .Select(m => m.Groups[1].Value)
                    .Order()
                    .ToArray();
                Check(
                    expected.SequenceEqual(actual),
                    "translation preserves placeholders: " + pair.Key
                );
                _ = CompositeFormat.Parse(pair.Value);
            }
            foreach (var folder in new[] { "Client", "SkillsExtended.Common" })
            foreach (
                var file in Directory.GetFiles(
                    Path.Combine(root.FullName, folder),
                    "*.cs",
                    SearchOption.AllDirectories
                )
            )
            foreach (
                Match match in Regex.Matches(
                    File.ReadAllText(file),
                    "LocalizedText\\.(?:Get|Message)\\(\\s*\"(SkillsExtended\\.[^\"]+)\""
                )
            )
            {
                var key = match.Groups[1].Value;
                if (!key.EndsWith('.'))
                    Check(english.ContainsKey(key), "client key exists: " + key);
            }
        }
        finally
        {
            LocalizedText.Resolver = null;
            CultureInfo.CurrentCulture = culture;
        }
        Console.WriteLine(
            $"Localization: {checks} fallback, language, format, catalog and authority checks passed."
        );
    }

    private static Dictionary<string, string> ReadLocale(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        // ToDictionary also rejects duplicate keys that ordinary deserialization could hide.
        return document
            .RootElement.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString());
    }
}
