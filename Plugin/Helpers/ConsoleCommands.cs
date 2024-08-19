using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using System;
using System.Linq;
using EFT.Console.Core;

namespace SkillsExtended.Helpers
{
    internal static class ConsoleCommands
    {
        public static void RegisterCommands()
        {
            ConsoleScreen.Processor.RegisterCommand("getAllWeaponIdsInInventory", new Action(GetAllWeaponIDsInInventory));
            
            ConsoleScreen.Processor.RegisterCommand("damage", new Action(DoDamage));
            ConsoleScreen.Processor.RegisterCommand("die", new Action(DoDie));
            ConsoleScreen.Processor.RegisterCommand("fracture", new Action(DoFracture));
            ConsoleScreen.Processor.RegisterCommand("fracture", new Action(DoFracture));

            ConsoleScreen.Processor.RegisterCommandGroup<Commands>();
        }

        internal class Commands
        {
            [ConsoleCommand("minigame", "", "Mini game practice")]
            public static void StartMiniGame([ConsoleArgument(50)] int chance)
            {
                Plugin.MiniGame.gameObject.SetActive(true);
                Plugin.MiniGame.ActivatePractice(chance);
            }
        }
        
        private static void GetAllWeaponIDsInInventory()
        {
            var weapons = Plugin.Session?.Profile?.Inventory?.AllRealPlayerItems;
            weapons = weapons.Where(x => x is Weapon);

            foreach (var weapon in weapons)
            {
                Plugin.Log.LogDebug($"Template ID: {weapon.TemplateId}, locale name: {weapon.LocalizedName()}");
            }
        }
        
        #region HEALTH
        private static void DoDamage()
        {
            var player = Singleton<GameWorld>.Instance.MainPlayer;
            DamageInfo Blunt = new DamageInfo();

            if (player == null) { return; }

            player.ActiveHealthController.ApplyDamage(EBodyPart.LeftLeg, 20, Blunt);
        }

        private static void DoDie()
        {
            var player = Singleton<GameWorld>.Instance.MainPlayer;
            DamageInfo Blunt = new DamageInfo();

            if (player == null) { return; }

            player.ActiveHealthController.ApplyDamage(EBodyPart.Head, int.MaxValue, Blunt);
        }

        private static void DoFracture()
        {
            var player = Singleton<GameWorld>.Instance.MainPlayer;

            if (player == null) { return; }

            player.ActiveHealthController.DoFracture(EBodyPart.LeftLeg);
        }
        #endregion HEALTH
    }
}