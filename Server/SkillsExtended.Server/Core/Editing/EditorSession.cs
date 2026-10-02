using System.Text.Json.Nodes;
using SkillsExtended.Config;
using SkillsExtended.Models;

namespace SkillsExtended.Core.Editing;

public sealed class EditorSession
{
    public ConfigSnapshot Draft { get; private set; }
    public ConfigSnapshot Baseline { get; private set; }
    public SkillsConfig Skills => Draft.Skills;
    public ServerConfig Server => Draft.Server;
    public Dictionary<string, string> Inputs { get; } = [];
    public Dictionary<string, string> InputErrors { get; } = [];
    public bool Busy { get; set; }
    public event Action? Changed;

    public EditorSession(ConfigSnapshot snapshot)
    {
        Draft = ConfigStore.Clone(snapshot);
        Baseline = ConfigStore.Clone(snapshot);
    }

    public void Reset(ConfigSnapshot snapshot)
    {
        Draft = ConfigStore.Clone(snapshot);
        Baseline = ConfigStore.Clone(snapshot);
        Inputs.Clear();
        InputErrors.Clear();
        Notify();
    }

    public void Notify() => Changed?.Invoke();

    public int ChangesFor(string key)
    {
        var skill = SkillCatalog.All.Single(s => s.Key == key);
        var paths = new HashSet<string>();
        CollectChanges(
            JsonNode.Parse(ConfigStore.Serialize(skill.Data(Baseline.Skills))),
            JsonNode.Parse(ConfigStore.Serialize(skill.Data(Skills))),
            key,
            paths
        );
        paths.UnionWith(
            InputErrors.Keys.Where(k => k.StartsWith(key + ".", StringComparison.Ordinal))
        );
        return paths.Count;
    }

    public int ChangeCount
    {
        get
        {
            var serverChanges = new HashSet<string>();
            CollectChanges(
                JsonNode.Parse(ConfigStore.Serialize(Baseline.Server)),
                JsonNode.Parse(ConfigStore.Serialize(Server)),
                "Server",
                serverChanges
            );
            return SkillCatalog.All.Sum(s => ChangesFor(s.Key)) + serverChanges.Count;
        }
    }
    public bool Dirty => ChangeCount > 0;

    private static void CollectChanges(
        JsonNode? before,
        JsonNode? after,
        string path,
        HashSet<string> paths
    )
    {
        if (before is JsonObject a && after is JsonObject b)
        {
            foreach (var key in a.Select(p => p.Key).Union(b.Select(p => p.Key)))
            {
                CollectChanges(a[key], b[key], path + "." + key, paths);
            }
            return;
        }
        // Set order does not change weapon lists or editor permissions.
        if (before is JsonArray aa && after is JsonArray bb)
        {
            var left = aa.Select(x => x!.ToJsonString()).ToHashSet();
            var right = bb.Select(x => x!.ToJsonString()).ToHashSet();
            paths.UnionWith(
                left.Except(right).Concat(right.Except(left)).Select(id => path + "." + id)
            );
            return;
        }
        if (!JsonNode.DeepEquals(before, after))
        {
            paths.Add(path);
        }
    }
}
