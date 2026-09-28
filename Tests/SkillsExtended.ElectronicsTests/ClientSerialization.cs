using System.Reflection;
using System.Runtime.Loader;
using Mono.Cecil;
using Newtonsoft.Json;
using SPTarkov.Server.Core.Models.Enums;

internal static class ClientSerialization
{
    public static void Verify()
    {
        var root = Environment.GetEnvironmentVariable("SKILLS_EFT_ROOT") ?? @"F:\SPT 4.1.x";
        AssemblyLoadContext.Default.Resolving += (_, name) =>
        {
            foreach (
                var directory in new[]
                {
                    "EscapeFromTarkov_Data/Managed",
                    "BepInEx/core",
                    "BepInEx/plugins/spt",
                }
            )
            {
                var file = Path.Combine(root, directory, name.Name + ".dll");
                if (File.Exists(file))
                {
                    return AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
                }
            }

            return null;
        };
        using var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.Combine(root, "EscapeFromTarkov_Data/Managed"));
        resolver.AddSearchDirectory(Path.Combine(root, "BepInEx/core"));
        resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(object).Assembly.Location));
        using var original = AssemblyDefinition.ReadAssembly(
            Path.Combine(root, "EscapeFromTarkov_Data/Managed/Assembly-CSharp.dll"),
            new ReaderParameters { AssemblyResolver = resolver }
        );
        // Start from the unpatched game assembly, not a dump containing an older local prepatch.
        // Invoke the production enum patch on an in-memory copy. Never overwrite installed assemblies.
        var patch = typeof(SkillsExtended.SkillsExtendedPatcher).GetMethod(
            "PatchHacking",
            BindingFlags.Static | BindingFlags.NonPublic
        );
        patch.Invoke(null, new object[] { original });
        // Reapplying must retain one named entry at each reserved numeric ID.
        patch.Invoke(null, new object[] { original });
        foreach (var (typeName, name, number, wireName) in new[]
        {
            ("EFT.ESkillId", "Hacking", 200, "200"),
            ("EFT.EBuffId", "HackingCoherence", 1028, "HackingCoherence"),
            ("EFT.EBuffId", "HackingStrength", 1029, "HackingStrength"),
            ("EFT.EBuffId", "HackingUtilitySlots", 1030, "HackingUtilitySlots"),
        })
        {
            var type = original.MainModule.GetType(typeName);
            var field = type.Fields.Single(f => f.HasConstant && Convert.ToInt32(f.Constant) == number);
            var attribute = field.CustomAttributes.Single(a => a.AttributeType.FullName == "EFT.JsonEnumNameAttribute");
            if (field.Name != name || (string)attribute.ConstructorArguments[0].Value != wireName
                || type.Fields.Any(f => f.Name.StartsWith("Electronics", StringComparison.Ordinal)))
            {
                throw new Exception($"Hacking identifier contract failed: {typeName}.{name} = {number}");
            }
        }
        using var stream = new MemoryStream();
        original.Write(stream);
        stream.Position = 0;
        var game = AssemblyLoadContext.Default.LoadFromStream(stream);
        var idType = game.GetType("EFT.ESkillId", true);
        if (Enum.GetUnderlyingType(idType) != typeof(byte))
        {
            throw new Exception("Skill enum storage changed.");
        }

        var value = Enum.ToObject(idType, 200);
        var converter = (JsonConverter)
            Activator.CreateInstance(
                game.GetType("EFT.EnumConverter`1", true).MakeGenericType(idType)
            );
        var wire = JsonConvert.SerializeObject(value, converter);
        if (wire != "\"200\"")
        {
            throw new Exception("Client skill ID lost numeric identity: " + wire);
        }

        var serverId = System.Text.Json.JsonSerializer.Deserialize<SkillTypes>(wire);
        if ((int)serverId != 200)
        {
            throw new Exception("Server rejected client skill ID.");
        }

        var serverWire = System.Text.Json.JsonSerializer.Serialize(serverId);
        var clientId = JsonConvert.DeserializeObject(serverWire, idType, converter);
        if (Convert.ToByte(clientId) != 200)
        {
            throw new Exception("Client rejected server numeric ID.");
        }

        Console.WriteLine(
            "Hacking skill/buff identifiers verified; actual game EnumConverter<byte> -> server SkillTypes -> game roundtrip passed (ID 200)."
        );
    }
}
