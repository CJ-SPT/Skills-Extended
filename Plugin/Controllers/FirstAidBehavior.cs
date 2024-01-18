using EFT;
using UnityEngine;
using Comfort.Common;
using Skill_Redux.Patches;

namespace SkillRedux.Controllers
{
    public class FirstAid : MonoBehaviour
    {
        GameWorld gameWorld { get => Singleton<GameWorld>.Instance; }

        Player player { get => gameWorld.MainPlayer; }

        SkillManager skillManager;

        void Awake()
        {
            new FirstAidSkillPatches.HealthControllerMedEffectPatch().Enable();
            new FirstAidSkillPatches.HealthEffectComponentPatch().Enable();
            new FirstAidSkillPatches.FirstAidEnablePatch().Enable();
        }

        void Update()
        {
            // Set skill manager instance
            if (skillManager == null && Plugin.Session?.Profile?.Skills != null)
            {
                skillManager = Plugin.Session.Profile.Skills;
                Plugin.Log.LogDebug("Skill Manager instance set.");
            }
        }

        public void ApplyExp()
        {
            var firstAid = skillManager.FirstAid;
            float xpGain = 1.5f;

            firstAid.SetCurrent(firstAid.Current + xpGain, true);
            
            if (firstAid.LevelProgress >= firstAid.LevelExp)
            {
                firstAid.SetLevel(firstAid.Level + 1);
            }
       
            Plugin.Log.LogDebug($"Skill: {firstAid.Id} Gained: {xpGain} exp.");
        }

        public float CalculateSpeedBonus()
        {
            var firstAid = skillManager.FirstAid;
            
            // 0.07% per level, Max 35%
            float bonus = 1f - (firstAid.Level * 0.007f);
            
            if (firstAid.IsEliteLevel)
            {
                // 15% Elite bonus
                bonus = bonus -  0.15f;
            }

            Plugin.Log.LogDebug($"{firstAid.Id} Bonus: {(1 - bonus) * 100}%, Is elite: {firstAid.IsEliteLevel}");
            
            return bonus;
        }
    }
}