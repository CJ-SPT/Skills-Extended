using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.Interactive;
using EFT.InventoryLogic;
using UnityEngine;

namespace SkillsExtended.Skills.LockPicking;

public static class LockPickingHelpers
{
    public const string PickTemplate = "6622c28aed7e3bc72e301e22";

    public static bool Supported(WorldInteractiveObject door) =>
        door
        && door is not KeycardDoor
        && !string.IsNullOrEmpty(door.KeyId)
        && !string.IsNullOrEmpty(door.Id)
        && !door.Id.StartsWith(SkillsExtended.Signals.SignalsIds.Prefix)
        && SkillsExtendedPlugin.Keys.KeyLocale.ContainsKey(door.KeyId);

    public static int GetLevelForDoor(string locationId, string doorId)
    {
        var maps = SkillsExtendedPlugin.SkillData?.LockPicking?.DoorPickLevels;
        if (maps == null || string.IsNullOrEmpty(doorId))
            return -1;
        var table = locationId?.ToLowerInvariant() switch
        {
            "factory4_day" or "factory4_night" => maps.Factory,
            "woods" => maps.Woods,
            "bigmap" => maps.Customs,
            "interchange" => maps.Interchange,
            "rezervbase" => maps.Reserve,
            "shoreline" => maps.Shoreline,
            "laboratory" => maps.Labs,
            "lighthouse" => maps.Lighthouse,
            "tarkovstreets" => maps.Streets,
            "sandbox" or "sandbox_high" => maps.GroundZero,
            "labyrinth" => maps.Labyrinth,
            _ => null,
        };
        return table != null && table.TryGetValue(doorId, out var level) ? level : -1;
    }

    public static IEnumerable<Key> Picks(Player player) =>
        player
            ?.Inventory?.GetPlayerItems(EPlayerItems.Equipment)
            .OfType<Key>()
            .Where(k =>
                k.TemplateId == PickTemplate
                && (
                    k.KeyComponent.Template.MaximumNumberOfUsage <= 0
                    || k.KeyComponent.NumberOfUsages < k.KeyComponent.Template.MaximumNumberOfUsage
                )
            )
        ?? Enumerable.Empty<Key>();

    public static IEnumerable<Item> GetLockPicksInInventory() =>
        Picks(Singleton<GameWorld>.Instance?.MainPlayer);
}
