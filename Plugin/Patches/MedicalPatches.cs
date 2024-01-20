using Aki.Reflection.Patching;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SkillsExtended.Helpers;
using System.Collections.Generic;
using System.Reflection;

namespace SkillsExtended.Patches
{
    internal class MedicalPatches
    {
        private static bool _isSurgery = false;
        private static bool _IsFieldMedicine = false;

        private static List<string> _fieldMedicineItemIdList = new List<string>
        {
            "544fb25a4bdc2dfb738b4567", //bandage
            "5751a25924597722c463c472", //army bandage
            "5e831507ea0a7c419c2f9bd9", //esmarch
            "60098af40accd37ef2175f27", //CAT
            "5e8488fa988a8701445df1e4", //calok-b
            "544fb3364bdc2d34748b456a", //splint
            "5af0454c86f7746bf20992e8", //alu splint
        };

        internal class EnableSkillsPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod() =>
                typeof(SkillManager).GetMethod("method_3", BindingFlags.NonPublic | BindingFlags.Instance);

            [PatchPostfix]
            public static void Postfix(SkillManager __instance)
            {
                AccessTools.Field(Utils.GetSkillType(), "Locked").SetValue(__instance.FirstAid, false);
                AccessTools.Field(Utils.GetSkillType(), "Locked").SetValue(__instance.FieldMedicine, false);
            }
        }

        internal class DoMedEffectPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod() =>
                typeof(ActiveHealthController).GetMethod("DoMedEffect");

            [PatchPrefix]
            public static void Prefix(ref Item item)
            {
                // We dont want to alter surgery with the first aid skill
                if (item is MedsClass meds)
                {
                    var healthEffectComp = meds.HealthEffectsComponent;

                    // Surgery item, dont adjust time
                    if (healthEffectComp.AffectsAny(EDamageEffectType.DestroyedPart))
                    {
                        Plugin.Log.LogDebug("Surgery effect, skipping time modification");
                        _isSurgery = true;
                    }       
                }

                if (_fieldMedicineItemIdList.Contains(item.TemplateId))
                {
                    Plugin.Log.LogDebug("Field Medicine Effect");
                    _IsFieldMedicine = true;
                    return;
                }

                _IsFieldMedicine = false;
                _isSurgery = false;
            }
        }

        internal class UseTimeForPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod() =>
                typeof(HealthEffectsComponent).GetMethod("UseTimeFor");

            [PatchPostfix]
            public static void Postfix(ref float __result)
            {
                if (!_isSurgery && !_IsFieldMedicine)
                {
                    Plugin.FAScript.ApplyFirstAidExp();
                    
                    __result = __result * Plugin.FAScript.CalculateFirstAidSpeedBonus();
                   
                    Plugin.Log.LogDebug($"First aid time {__result} seconds");
                    
                    _isSurgery = false;
                    _IsFieldMedicine = false;
                    return;
                }

                if (!_isSurgery && _IsFieldMedicine)
                {
                    Plugin.FAScript.ApplyFieldMedicineExp();

                    __result = __result * Plugin.FAScript.CalculateFirstAidSpeedBonus();

                    Plugin.Log.LogDebug($"Field Medicine time {__result} seconds");

                    _isSurgery = false;
                    _IsFieldMedicine = false;
                    return;
                }

                Plugin.Log.LogDebug("UseTimeFor: No speed bonus applied");              
            }
        }
    }
}