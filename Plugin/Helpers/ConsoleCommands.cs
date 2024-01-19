using EFT;
using EFT.UI;
using System;
using Comfort.Common;
using SkillsExtended;
using SkillsExtended.Controllers;

namespace SkillRedux.Helpers
{
    internal static class ConsoleCommands
    {
        public static void RegisterCommands()
        {
            ConsoleScreen.Processor.RegisterCommand("damage", new Action(DoDamage));
            ConsoleScreen.Processor.RegisterCommand("die", new Action(DoDie));
            ConsoleScreen.Processor.RegisterCommand("increaseFirstAid", new Action(DoIncreaseFirstAidLevel));
            ConsoleScreen.Processor.RegisterCommand("decreaseFirstAid", new Action(DoDecreaseFirstAidLevel));
        }

        public static void DoDamage() 
        {
            var player = Singleton<GameWorld>.Instance.MainPlayer;
            DamageInfo Blunt = new DamageInfo();

            if (player == null) { return; }

            player.ActiveHealthController.ApplyDamage(EBodyPart.Chest, 50, Blunt);
        }

        public static void DoDie()
        {
            var player = Singleton<GameWorld>.Instance.MainPlayer;
            DamageInfo Blunt = new DamageInfo();

            if (player == null) { return; }

            player.ActiveHealthController.ApplyDamage(EBodyPart.Head, int.MaxValue, Blunt);
        }

        public static void DoIncreaseFirstAidLevel()
        {
            var firstAid = Plugin.Session.Profile.Skills.FirstAid;

            if (firstAid == null) { return; }

            firstAid.SetLevel(firstAid.Level + 1);
        }
        public static void DoDecreaseFirstAidLevel()
        {
            var firstAid = Plugin.Session.Profile.Skills.FirstAid;

            if (firstAid == null) { return; }

            firstAid.SetLevel(firstAid.Level - 1);
        }
    }
}
