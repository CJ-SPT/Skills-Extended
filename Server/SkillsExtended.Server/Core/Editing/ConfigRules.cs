using System.Globalization;
using SkillsExtended.Config;
using SkillsExtended.Config.Skills;

namespace SkillsExtended.Core.Editing;

public static class ConfigRules
{
    public static void RequireStructure(SkillsConfig config)
    {
        foreach (var skill in SkillCatalog.All)
        {
            var data =
                skill.Data(config)
                ?? throw new InvalidDataException(
                    $"Missing {skill.Name} section. Restore it in SkillsConfig.json."
                );
            if (data is WeaponSkillData { Weapons: null })
            {
                throw new InvalidDataException($"Missing {skill.Name} weapon list.");
            }
        }

        if (config.LockPicking.XpTable is null || config.LockPicking.DoorPickLevels is null)
        {
            throw new InvalidDataException("Missing lockpicking tables.");
        }

        foreach (var map in typeof(DoorPickLevels).GetProperties())
        {
            if (map.GetValue(config.LockPicking.DoorPickLevels) is null)
            {
                throw new InvalidDataException($"Missing door table: {map.Name}.");
            }
        }
    }

    public static List<string> Validate(SkillsConfig config)
    {
        var errors = new List<string>();
        try
        {
            config.Hacking?.Validate();
            config.LockPicking?.Validate();
            config.SignalsIntelligence?.Validate();
        }
        catch (ArgumentException ex)
        {
            errors.Add(ex.Message);
        }

        foreach (var skill in SkillCatalog.All)
        {
            var data = skill.Data(config);
            if (data is null)
            {
                errors.Add($"{skill.Name}: missing configuration section.");
                continue;
            }

            foreach (var field in skill.Fields.Where(f => !f.IsBoolean))
            {
                var error = field.Parse(
                    Convert.ToString(field.Property.GetValue(data), CultureInfo.CurrentCulture)!,
                    out _
                );
                if (error is not null)
                {
                    errors.Add($"{skill.Name} / {field.Label}: {error}");
                }
            }

            if (
                data is WeaponSkillData weapon
                && (weapon.Weapons is null || weapon.Weapons.Any(string.IsNullOrWhiteSpace))
            )
            {
                errors.Add($"{skill.Name}: weapon IDs must not be empty.");
            }
        }

        var locks = config.LockPicking;
        if (locks is null)
        {
            return errors;
        }

        if (locks.XpTable is null)
        {
            errors.Add("Lock Picking: missing XP table.");
        }
        else
        {
            foreach (var (key, value) in locks.XpTable)
            {
                if (
                    !int.TryParse(
                        key,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var level
                    )
                    || level < 0
                    || key != level.ToString(CultureInfo.InvariantCulture)
                )
                {
                    errors.Add(
                        $"Lock Picking: XP level '{key}' must be a nonnegative integer without leading zeros."
                    );
                }

                if (!float.IsFinite(value) || value < 0)
                {
                    errors.Add($"Lock Picking: XP for level {key} must be finite and nonnegative.");
                }
            }
        }

        if (locks.DoorPickLevels is null)
        {
            errors.Add("Lock Picking: missing map tables.");
        }
        else
        {
            foreach (var map in typeof(DoorPickLevels).GetProperties())
            {
                if (map.GetValue(locks.DoorPickLevels) is not Dictionary<string, int> doors)
                {
                    errors.Add($"Lock Picking: missing {map.Name} table.");
                    continue;
                }

                foreach (var (id, level) in doors)
                {
                    if (string.IsNullOrWhiteSpace(id) || id != id.Trim() || level < 0)
                    {
                        errors.Add(
                            $"{map.Name}: door IDs must be nonempty and levels nonnegative."
                        );
                    }
                }
            }
        }

        return errors;
    }

    public static IEnumerable<string> Warnings(SkillsConfig config)
    {
        if (config.LockPicking?.DoorPickLevels is null || config.LockPicking.XpTable is null)
        {
            yield break;
        }

        foreach (var map in typeof(DoorPickLevels).GetProperties())
        {
            if (
                map.GetValue(config.LockPicking.DoorPickLevels) is not Dictionary<string, int> doors
            )
            {
                continue;
            }

            foreach (
                var level in doors
                    .Values.Distinct()
                    .Where(level =>
                        !config.LockPicking.XpTable.ContainsKey(
                            level.ToString(CultureInfo.InvariantCulture)
                        )
                    )
            )
            {
                yield return $"{map.Name}: lock level {level} has no XP entry; those locks award no XP.";
            }
        }
    }
}
