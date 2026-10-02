using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using EFT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkillsExtended.Skills.Signals;

var gameRoot = Environment.GetEnvironmentVariable("SKILLS_EFT_ROOT") ?? @"F:\SPT 4.1.x";
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var path = Path.Combine(gameRoot, "EscapeFromTarkov_Data", "Managed", name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
InventoryChecks.Run();

internal static class InventoryChecks
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Run()
    {
        var checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            checks++;
        }
        ItemDescriptor Item(string id, List<ItemComponentDescriptor> components = null) => new()
        {
            Id = id, TemplateId = "5448bf274bdc2dfc2f8b456a", StackCount = 1,
            SpawnedInSession = true, Components = components,
        };
        var child = Item("000000000000000000000002", new()
        {
            new ResourceItemComponentDescriptor { Resource = 37.25f },
            new MedKitComponentDescriptor { HpResource = 123.5f },
            new KeyComponentDescriptor { NumberOfUsages = 4 },
        });
        child.StackCount = 7;
        var root = Item("000000000000000000000001");
        root.Grids = new()
        {
            new() { GridNumber = 0, ContainedItems = new()
            {
                new() { Item = child, X = 3, Y = 5, Horizontal = true },
            } },
        };
        root.Slots = new() { new() { SlotNumber = 2, ContainedItem = Item("000000000000000000000003") } };
        root.StackSlots = new() { new() { SlotNumber = 1, ContainedItems = new() { Item("000000000000000000000004") } } };

        var legacy = JsonConvert.SerializeObject(root, EftJsonConverters.Converters);
        try
        {
            JsonConvert.DeserializeObject<ItemDescriptor>(legacy, EftJsonConverters.Converters);
            throw new Exception("Legacy serializer no longer reproduces the resource-component failure.");
        }
        catch (JsonSerializationException e)
        {
            Check(e.Message.Contains("ItemComponentDescriptor") && e.Path.Contains("Components"),
                "Reproduce the reported abstract component failure using actual game types.");
        }

        var wire = SignalsInventoryCodec.Serialize(root);
        // Exercise the existing JSON string transport, including escaping and roundtrip.
        var envelope = JsonConvert.SerializeObject(new { Inventory = wire });
        var restored = SignalsInventoryCodec.Deserialize(JObject.Parse(envelope).Value<string>("Inventory"));
        Check(restored.Id == root.Id && restored.TemplateId == root.TemplateId, "Container identity preserved.");
        Check(restored.Components == null, "Null component lists preserved.");
        Check(restored.SpawnedInSession && restored.StackCount == 1, "Root raid flags preserved.");
        var grid = restored.Grids.Single();
        var entry = grid.ContainedItems.Single();
        var loot = entry.Item;
        Check(grid.GridNumber == 0 && entry.X == 3 && entry.Y == 5 && entry.Horizontal, "Grid position and orientation preserved.");
        Check(loot.Id == child.Id && loot.TemplateId == child.TemplateId && loot.StackCount == 7 && loot.SpawnedInSession,
            "Loot identity, stack and raid flags preserved.");
        Check(loot.Components[0] is ResourceItemComponentDescriptor { Resource: 37.25f }, "Resource component reconstructed.");
        Check(loot.Components[1] is MedKitComponentDescriptor { HpResource: 123.5f }, "Medical component reconstructed.");
        Check(loot.Components[2] is KeyComponentDescriptor { NumberOfUsages: 4 }, "Key uses reconstructed.");
        Check(restored.Slots.Single().ContainedItem.Id == root.Slots[0].ContainedItem.Id, "Nested slot identity preserved.");
        Check(restored.StackSlots.Single().ContainedItems.Single().Id == root.StackSlots[0].ContainedItems[0].Id,
            "Nested stack-slot identity preserved.");
        Check(SignalsInventoryCodec.Serialize(restored) == wire, "Native descriptor roundtrip is lossless.");
        // A later snapshot must retain consumed resources and removed loot, not recreate manifest items.
        ((ResourceItemComponentDescriptor)loot.Components[0]).Resource = 2.5f;
        restored.Slots.Clear();
        var updated = SignalsInventoryCodec.Deserialize(SignalsInventoryCodec.Serialize(restored));
        Check(((ResourceItemComponentDescriptor)updated.Grids[0].ContainedItems[0].Item.Components[0]).Resource == 2.5f
            && updated.Slots.Count == 0, "Current host inventory changes preserved.");
        var separator = wire.IndexOf(':') + 1;
        var bytes = Convert.FromBase64String(wire[separator..]);
        Array.Resize(ref bytes, bytes.Length + 1);
        var trailing = wire[..separator] + Convert.ToBase64String(bytes);
        foreach (var invalid in new[] { legacy, "eft-item-v1:!", wire[..^4], trailing })
        {
            try { SignalsInventoryCodec.Deserialize(invalid); }
            catch (Exception e) when (e is InvalidDataException or IOException or FormatException or ArgumentException)
            { checks++; continue; }
            throw new Exception("Invalid or incompatible inventory was accepted.");
        }
        Console.WriteLine($"Signals inventory: {checks} real EFT codec checks passed.");
    }
}
