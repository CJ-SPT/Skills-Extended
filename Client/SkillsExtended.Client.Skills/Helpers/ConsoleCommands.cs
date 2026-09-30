using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Console.Core;
using EFT.InventoryLogic;
using EFT.UI;
using SkillsExtended.Skills.LockPicking;
using SkillsExtended.Utils;

namespace SkillsExtended.Helpers;

internal static class ConsoleCommands
{
    public static void RegisterCommands()
    {
        ConsoleScreen.Processor.RegisterCommand(
            "getAllWeaponIdsInInventory",
            GetAllWeaponIDsInInventory
        );

        ConsoleScreen.Processor.RegisterCommand("damage", DoDamage);
        ConsoleScreen.Processor.RegisterCommand("die", DoDie);
        ConsoleScreen.Processor.RegisterCommand("fracture", DoFracture);

        ConsoleScreen.Processor.RegisterCommandGroup<Commands>();
    }

    internal class Commands
    {
        [ConsoleCommand("minigame", "", "Mini game practice")]
        public static void StartMiniGame([ConsoleArgument(50)] int chance)
        {
            LockPickingGame.Practice(System.Math.Clamp(chance, 1, 5), 0, 1);
        }
    }

    private static void GetAllWeaponIDsInInventory()
    {
        var side = GameUtils.IsScav() ? EPlayerSide.Savage : EPlayerSide.Usec;
        var weapons = GameUtils
            .GetProfile(side)
            ?.Inventory?.AllRealPlayerItems.Where(x => x is Weapon);

        foreach (var weapon in weapons)
        {
            SkillsExtendedPlugin.Log.LogDebug(
                $"Template ID: {weapon.TemplateId}, locale name: {weapon.LocalizedName()}"
            );
        }
    }

    #region HEALTH

    private static void DoDamage()
    {
        var player = Singleton<GameWorld>.Instance.MainPlayer;
        var Blunt = new DamageInfo();

        if (!player)
        {
            return;
        }

        player.ActiveHealthController.ApplyDamage(EBodyPart.LeftLeg, 20, Blunt);
    }

    private static void DoDie()
    {
        var player = Singleton<GameWorld>.Instance.MainPlayer;
        var Blunt = new DamageInfo();

        if (!player)
        {
            return;
        }

        player.ActiveHealthController.ApplyDamage(EBodyPart.Head, int.MaxValue, Blunt);
    }

    private static void DoFracture()
    {
        var player = Singleton<GameWorld>.Instance.MainPlayer;

        if (!player)
        {
            return;
        }

        player.ActiveHealthController.DoFracture(EBodyPart.LeftLeg);
    }

    #endregion HEALTH
}
