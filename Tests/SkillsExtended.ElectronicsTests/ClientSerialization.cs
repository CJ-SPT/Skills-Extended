using System.Reflection;
using System.Runtime.Loader;
using Mono.Cecil;
using Newtonsoft.Json;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;

internal static class ClientSerialization
{
    public static void Verify()
    {
        if (!Enum.TryParse<SkillTypes>("Hacking", out var injected) || (int)injected != 200)
        {
            throw new Exception(
                "Server enum extension missing. Run Run-SerializationChecks.ps1 to prepare the isolated server fixture."
            );
        }
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
        var localBase = original.MainModule.GetType("EFT.LocalGame").BaseType.Resolve();
        var lootBoundary = localBase.Methods.Single(m => m.Name == "SpawnLoot");
        if (
            lootBoundary.ReturnType.FullName != "System.Threading.Tasks.Task"
            || lootBoundary.Parameters.Count != 1
        )
            throw new Exception("Signals loot initialization boundary changed.");
        var nativeWorld = original.MainModule.GetType("EFT.World");
        var nativeCacheInteract = original
            .MainModule.GetType("EFT.Interactive.LootableContainer")
            .Methods.Single(m =>
                m.Name == "Interact"
                && m.Parameters.Count == 1
                && m.Parameters[0].ParameterType.FullName == "EFT.Interactive.InteractionResult"
            );
        if (
            !nativeCacheInteract.HasBody
            || nativeCacheInteract.ReturnType.FullName != "System.Void"
        )
            throw new Exception("Signals cache lock patch target changed.");
        foreach (
            var name in new[]
            {
                "_interactiveObjectsDictionary",
                "_interactables",
                "_interactableObjectsForNetSync",
            }
        )
            if (!nativeWorld.Fields.Any(f => f.Name == name))
                throw new Exception("Signals native interaction registry changed: " + name);
        var fikaPath = Path.Combine(root, "BepInEx/plugins/Fika/Fika.Core.dll");
        if (File.Exists(fikaPath))
        {
            using var fika = AssemblyDefinition.ReadAssembly(fikaPath);
            if (
                fika.MainModule.GetType("Fika.Core.Main.GameMode.CoopGame").BaseType.FullName
                != original.MainModule.GetType("EFT.LocalGame").BaseType.FullName
            )
                throw new Exception("Fika and solo loot initialization boundaries differ.");
        }
        var headlessRoot = Environment.GetEnvironmentVariable("SKILLS_EFT_HEADLESS_ROOT")
            ?? Path.GetFullPath(Path.Combine(root, "../" + Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar)) + " - Headless"));
        var headlessPath = Path.Combine(headlessRoot, "BepInEx/plugins/Fika/Fika.Headless.dll");
        if (File.Exists(headlessPath))
        {
            using var headless = AssemblyDefinition.ReadAssembly(headlessPath);
            var headlessGame = headless.MainModule.GetType("Fika.Headless.Classes.GameMode.HeadlessGame");
            var loadLoot = headlessGame?.Methods.SingleOrDefault(m => m.Name == "LoadLoot"
                && m.Parameters.Count == 1
                && m.Parameters[0].ParameterType.FullName == lootBoundary.Parameters[0].ParameterType.FullName);
            if (headlessGame?.BaseType.FullName != "EFT.AbstractGame"
                || loadLoot?.ReturnType.FullName != "System.Threading.Tasks.Task"
                || headlessGame.Properties.SingleOrDefault(p => p.Name == "GameWorld")?.PropertyType.FullName != "EFT.GameWorld")
                throw new Exception("Fika headless loot initialization boundary changed.");
            Console.WriteLine("Installed Fika headless loot boundary and world property passed.");
        }
        // Start from the unpatched game assembly, not a dump containing an older local prepatch.
        // Invoke the production enum patch on an in-memory copy. Never overwrite installed assemblies.
        var patch = typeof(SkillsExtended.SkillsExtendedPatcher).GetMethod(
            "PatchHacking",
            BindingFlags.Static | BindingFlags.NonPublic
        );
        patch.Invoke(null, new object[] { original });
        // Reapplying must retain one named entry at each reserved numeric ID.
        patch.Invoke(null, new object[] { original });
        foreach (
            var (typeName, name, number, wireName) in new[]
            {
                ("EFT.ESkillId", "Hacking", 200, "Hacking"),
                ("EFT.EBuffId", "HackingCoherence", 1028, "HackingCoherence"),
                ("EFT.EBuffId", "HackingStrength", 1029, "HackingStrength"),
                ("EFT.EBuffId", "HackingUtilitySlots", 1030, "HackingUtilitySlots"),
            }
        )
        {
            var type = original.MainModule.GetType(typeName);
            var field = type.Fields.Single(f =>
                f.HasConstant && Convert.ToInt32(f.Constant) == number
            );
            var attribute = field.CustomAttributes.Single(a =>
                a.AttributeType.FullName == "EFT.JsonEnumNameAttribute"
            );
            if (
                field.Name != name
                || (string)attribute.ConstructorArguments[0].Value != wireName
                || type.Fields.Any(f => f.Name.StartsWith("Electronics", StringComparison.Ordinal))
            )
            {
                throw new Exception(
                    $"Hacking identifier contract failed: {typeName}.{name} = {number}"
                );
            }
        }
        var signalsPatch = typeof(SkillsExtended.SkillsExtendedPatcher).GetMethod(
            "PatchSignals",
            BindingFlags.Static | BindingFlags.NonPublic
        )!;
        signalsPatch.Invoke(null, new object[] { original });
        signalsPatch.Invoke(null, new object[] { original });
        foreach (
            var pair in new[]
            {
                ("EFT.ESkillId", "SignalsIntelligence", 201),
                ("EFT.EBuffId", "SignalsBearingAccuracy", 1031),
                ("EFT.EBuffId", "SignalsTuningTolerance", 1032),
                ("EFT.EBuffId", "SignalsReadingMemory", 1033),
            }
        )
        {
            var field = original
                .MainModule.GetType(pair.Item1)
                .Fields.Single(f => f.HasConstant && Convert.ToInt32(f.Constant) == pair.Item3);
            if (field.Name != pair.Item2)
                throw new Exception("Signals enum contract failed.");
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
        // Raid saves use EftJsonConverters, whose fallback for ESkillId is StringEnumConverter.
        // The explicit EFT.EnumConverter below is not the normal raid-save path.
        var saveConverters = (JsonConverter[])
            game.GetType("EFT.EftJsonConverters", true).GetField("Converters").GetValue(null);
        var saveWire = JsonConvert.SerializeObject(value, saveConverters);
        if (saveWire != "\"Hacking\"")
        {
            throw new Exception("Raid-save skill ID lost numeric identity: " + saveWire);
        }

        var descriptorType = game.GetType("EFT.SkillsDescriptor+SkillInfoDescriptor", true);
        var descriptors = Array.CreateInstance(descriptorType, 53);
        var stockIds = Enum.GetValues(idType)
            .Cast<object>()
            .Where(id => Convert.ToInt32(id) != 200)
            .ToArray();
        foreach (var stockId in stockIds)
        {
            if (
                JsonConvert.SerializeObject(stockId, saveConverters)
                != JsonConvert.SerializeObject(stockId.ToString())
            )
            {
                throw new Exception("Stock skill wire name changed: " + stockId);
            }
        }

        for (var i = 0; i < descriptors.Length; i++)
        {
            var descriptor = Activator.CreateInstance(descriptorType);
            descriptorType.GetField("Id").SetValue(descriptor, i == 52 ? value : stockIds[i]);
            descriptorType.GetField("Progress").SetValue(descriptor, i == 52 ? 321f : 123f);
            descriptorType.GetField("PointsEarnedDuringSession").SetValue(descriptor, 7f);
            descriptors.SetValue(descriptor, i);
        }

        var requestWire = JsonConvert.SerializeObject(
            new { results = new { profile = new { Skills = new { Common = descriptors } } } },
            saveConverters
        );
        var json = new JsonUtil([new SptJsonConverterRegistrator()]);
        foreach (var stockId in Enum.GetValues<SkillTypes>())
        {
            var stockSkill = new SPTarkov.Server.Core.Models.Eft.Common.Tables.CommonSkill
            {
                Id = stockId,
            };
            using var stockJson = System.Text.Json.JsonDocument.Parse(json.Serialize(stockSkill));
            if (
                stockJson.RootElement.GetProperty("Id").GetString() != stockId.ToString()
                || json.Deserialize<SPTarkov.Server.Core.Models.Eft.Common.Tables.CommonSkill>(
                    json.Serialize(stockSkill)
                ).Id != stockId
            )
            {
                throw new Exception("Server stock skill serialization changed: " + stockId);
            }
        }
        foreach (var oldWire in new[] { "200", "\"200\"", "\"Hacking\"" })
        {
            var oldSkill =
                json.Deserialize<SPTarkov.Server.Core.Models.Eft.Common.Tables.CommonSkill>(
                    "{\"Id\":"
                        + oldWire
                        + ",\"Progress\":321,\"PointsEarnedDuringSession\":7,\"LastAccess\":1234}"
                );
            foreach (var indented in new[] { false, true })
            {
                var savedWire = json.Serialize(oldSkill, indented);
                using var savedJson = System.Text.Json.JsonDocument.Parse(savedWire);
                var savedId = savedJson.RootElement.GetProperty("Id");
                var loaded =
                    json.Deserialize<SPTarkov.Server.Core.Models.Eft.Common.Tables.CommonSkill>(
                        savedWire
                    );
                if (
                    savedId.ValueKind != System.Text.Json.JsonValueKind.String
                    || savedId.GetString() != "Hacking"
                    || (int)loaded.Id != 200
                    || loaded.Progress != 321
                    || loaded.PointsEarnedDuringSession != 7
                    || loaded.LastAccess != 1234
                )
                {
                    throw new Exception(
                        "Existing Hacking skill failed string serialization/progress preservation: "
                            + savedWire
                    );
                }
            }
        }
        var request = (EndLocalRaidRequestData)
            json.Deserialize(requestWire, typeof(EndLocalRaidRequestData));
        var skills = request.Results.Profile.Skills.Common.ToArray();
        if (
            skills.Length != 53
            || (int)skills[52].Id != 200
            || skills[52].Progress != 321
            || skills[52].PointsEarnedDuringSession != 7
            || skills.Take(52).Any(s => s.Progress != 123)
        )
        {
            throw new Exception("Raid-end request lost skill progress.");
        }

        var savedRequest = json.Deserialize<EndLocalRaidRequestData>(json.Serialize(request));
        var savedSkills = savedRequest.Results.Profile.Skills.Common;
        using (var savedJson = System.Text.Json.JsonDocument.Parse(json.Serialize(savedSkills)))
        {
            var hackingId = savedJson.RootElement[52].GetProperty("Id");
            if (
                hackingId.ValueKind != System.Text.Json.JsonValueKind.String
                || hackingId.GetString() != "Hacking"
            )
            {
                throw new Exception(
                    "Server must write Hacking as a string ID for profile tools: " + hackingId
                );
            }
        }
        var loadedDescriptors = (Array)
            JsonConvert.DeserializeObject(
                json.Serialize(savedSkills),
                descriptors.GetType(),
                saveConverters
            );
        var loadedHacking = loadedDescriptors.GetValue(52);
        if (
            Convert.ToInt32(descriptorType.GetField("Id").GetValue(loadedHacking)) != 200
            || (float)descriptorType.GetField("Progress").GetValue(loadedHacking) != 321f
        )
        {
            throw new Exception("Saved Hacking progress did not reload in the client.");
        }

        var converter = (JsonConverter)
            Activator.CreateInstance(
                game.GetType("EFT.EnumConverter`1", true).MakeGenericType(idType)
            );
        var wire = JsonConvert.SerializeObject(value, converter);
        if (wire != "\"Hacking\"")
        {
            throw new Exception("Client skill ID lost numeric identity: " + wire);
        }

        var serverId = json.Deserialize<SkillTypes>(wire);
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

        var signalValue = Enum.ToObject(idType, 201);
        var signalDescriptor = Activator.CreateInstance(descriptorType);
        descriptorType.GetField("Id").SetValue(signalDescriptor, signalValue);
        descriptorType.GetField("Progress").SetValue(signalDescriptor, 417f);
        var signalRequestWire = JsonConvert.SerializeObject(
            new
            {
                results = new
                {
                    profile = new { Skills = new { Common = new[] { signalDescriptor } } },
                },
            },
            saveConverters
        );
        var signalRequest = json.Deserialize<EndLocalRaidRequestData>(signalRequestWire);
        var savedSignal = json.Deserialize<EndLocalRaidRequestData>(json.Serialize(signalRequest))
            .Results.Profile.Skills.Common.Single();
        var back = JsonConvert.DeserializeObject(
            json.Serialize(savedSignal),
            descriptorType,
            saveConverters
        );
        if (
            (int)savedSignal.Id != 201
            || savedSignal.Progress != 417
            || Convert.ToInt32(descriptorType.GetField("Id").GetValue(back)) != 201
            || JsonConvert.SerializeObject(signalValue, converter) != "\"SignalsIntelligence\""
        )
            throw new Exception("Signals raid-save/profile/client roundtrip failed.");
        Console.WriteLine(
            "Signals Intelligence 201: real client converter, raid-end request, server persistence and client reload passed."
        );

        Console.WriteLine(
            "Hacking identifiers verified; actual EFT save converters -> SPT raid-end request -> saved profile -> client reload passed (Hacking = 200, index 52). Stock skill names, legacy numeric saves, compact/indented JSON, and explicit EnumConverter roundtrip passed."
        );
    }
}
