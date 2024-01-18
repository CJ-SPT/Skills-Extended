using EFT;
using EFT.UI;
using System;
using Comfort.Common;

namespace Skill_Redux.Helpers
{
    internal static class ConsoleCommands
    {
        public static void RegisterCommands()
        {
            ConsoleScreen.Processor.RegisterCommand("damage", new Action(DoDamage));
            ConsoleScreen.Processor.RegisterCommand("die", new Action(DoDie));
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
    }
}
