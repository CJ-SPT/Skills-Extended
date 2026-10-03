using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using SkillsExtended.Config;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace SkillsExtended.Core;

[Injectable(InjectionType.Singleton, OnLoadOrder.PostLoad + 1)]
public sealed class NativeSkillSettings(GlobalTable globals, ConfigController config) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        await config.EnsureLoadedAsync();
        var state = new NativeSkillOverrideState(globals.Configuration.SkillsSettings);
        config.NativeBonusDefaults = state.Defaults;
        state.Apply(config.SkillsConfig.NativeSkills);
        config.Saved += snapshot => state.Apply(snapshot.NativeSkills);
    }
}

public sealed class NativeSkillOverrideState
{
    private readonly Dictionary<string, (object Owner, PropertyInfo Property, object? Original)> _targets = [];
    private readonly HashSet<string> _applied = [];
    public IReadOnlyDictionary<string, float> Defaults { get; }

    public NativeSkillOverrideState(object settings)
    {
        var defaults = NativeSkillCatalog.Defaults.ToDictionary(p => p.Key, p => p.Value);
        foreach (var field in NativeSkillCatalog.All.SelectMany(s => s.Fields).Where(f => f.BuffId == 0))
        {
            var (owner, property) = Resolve(settings, field.Key);
            _targets[field.Key] = (owner, property, property.GetValue(owner));
            defaults[field.Key] = (float)(Convert.ToDecimal(property.GetValue(owner), CultureInfo.InvariantCulture)
                / Convert.ToDecimal(field.Scale, CultureInfo.InvariantCulture));
        }
        var recoil = Resolve(settings, "WeaponSkillRecoilBonusPerLevel");
        defaults["Buff.66.PerLevel"] = (float)(Convert.ToDecimal(recoil.Property.GetValue(recoil.Owner), CultureInfo.InvariantCulture) * 100);
        Defaults = defaults;
    }

    // Resolve the server model's JSON names, which can differ from its C# property names.
    public static (object Owner, PropertyInfo Property) Resolve(object root, string path)
    {
        var parts = path.Split('.');
        var owner = root;
        for (var i = 0; i < parts.Length; i++)
        {
            var property = owner.GetType().GetProperties().Single(p =>
                (p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name) == parts[i]);
            if (i == parts.Length - 1) return (owner, property);
            owner = property.GetValue(owner) ?? throw new InvalidDataException("Missing skill setting " + path);
        }
        throw new InvalidDataException("Invalid skill setting " + path);
    }

    public void Apply(NativeSkillData data)
    {
        data.Validate();
        foreach (var path in _applied)
        {
            var target = _targets[path];
            target.Property.SetValue(target.Owner, target.Original);
        }
        _applied.Clear();
        foreach (var skill in NativeSkillCatalog.All)
        {
            if (!data.Overrides.TryGetValue(skill.Key, out var values)) continue;
            foreach (var field in skill.Fields.Where(f => f.BuffId == 0 && values.ContainsKey(f.Key)))
            {
                var target = _targets[field.Key];
                target.Property.SetValue(target.Owner, Convert.ChangeType((double)values[field.Key] * field.Scale,
                    Nullable.GetUnderlyingType(target.Property.PropertyType) ?? target.Property.PropertyType, CultureInfo.InvariantCulture));
                _applied.Add(field.Key);
            }
        }
    }
}
