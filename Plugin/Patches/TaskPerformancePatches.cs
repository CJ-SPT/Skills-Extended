using Aki.Reflection.Patching;
using EFT.UI;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;

namespace SkillsExtended.Patches
{
    internal class FinishQuestPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(GClass2023).GetMethod("FinishQuest", BindingFlags.Public | BindingFlags.Instance);

        [PatchPostfix]
        public static void Postfix()
        {
            var skills = Plugin.Session.Profile.Skills;

            skills.Taskperformance.SetCurrent(skills.Taskperformance.Current + 50f);
        }
    }
}
