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

        SkillManager _playerSkillManager;
        SkillManager _ScavSkillManager;

        void Awake()
        {
            new FirstAidSkillPatches.HealthControllerMedEffectPatch().Enable();
            new FirstAidSkillPatches.HealthEffectComponentPatch().Enable();
            new FirstAidSkillPatches.FirstAidEnablePatch().Enable();
        }

        void Update()
        {
            // Set skill manager instance
            if (_playerSkillManager == null && Plugin.Session?.Profile?.Skills != null)
            {
                _playerSkillManager = Plugin.Session.Profile.Skills;
                _ScavSkillManager = Plugin.Session.ProfileOfPet.Skills;
                Plugin.Log.LogDebug("Skill Manager instances set.");
            }
        }

        public void ApplyExp()
        {
            float xpGain = 1.5f;

            if (player.Side == EPlayerSide.Usec || player.Side == EPlayerSide.Bear)
            {
                _playerSkillManager.FirstAid.SetCurrent(_playerSkillManager.FirstAid.Current + xpGain, true);

                if (_playerSkillManager.FirstAid.LevelProgress >= _playerSkillManager.FirstAid.LevelExp)
                {
                    _playerSkillManager.FirstAid.SetLevel(_playerSkillManager.FirstAid.Level + 1);
                }

                Plugin.Log.LogDebug($"Skill: {_playerSkillManager.FirstAid.Id} Side: {player.Side} Gained: {xpGain} exp.");
            }
            else if (player.Side == EPlayerSide.Savage)
            {
                _ScavSkillManager.FirstAid.SetCurrent(_ScavSkillManager.FirstAid.Current + xpGain, true);

                if (_ScavSkillManager.FirstAid.LevelProgress >= _ScavSkillManager.FirstAid.LevelExp)
                {
                    _ScavSkillManager.FirstAid.SetLevel(_ScavSkillManager.FirstAid.Level + 1);
                }

                Plugin.Log.LogDebug($"Skill: {_ScavSkillManager.FirstAid.Id} Side: {player.Side} Gained: {xpGain} exp.");
            }
            else
            {
                Plugin.Log.LogDebug($"No XP gain occured. Something went horribly wrong: Invalid Player side.");
            }   
        }

        public float CalculateSpeedBonus()
        {
            float bonus;

            if (player.Side != EPlayerSide.Savage)
            {
                // 0.07% per level, Max 35%
                bonus = 1f - (_playerSkillManager.FirstAid.Level * 0.007f);

                if (_playerSkillManager.FirstAid.IsEliteLevel)
                {
                    // 15% Elite bonus
                    bonus = bonus - 0.15f;
                }
                
                Plugin.Log.LogDebug($"{_playerSkillManager.FirstAid.Id} Bonus: {(1 - bonus) * 100}%, Is elite: {_playerSkillManager.FirstAid.IsEliteLevel}");
                
                return bonus;
            }
            else
            {
                // 0.07% per level, Max 35%
                bonus = 1f - (_ScavSkillManager.FirstAid.Level * 0.007f);

                if (_ScavSkillManager.FirstAid.IsEliteLevel)
                {
                    // 15% Elite bonus
                    bonus = bonus - 0.15f;
                }

                Plugin.Log.LogDebug($"{_ScavSkillManager.FirstAid.Id} Bonus: {(1 - bonus) * 100}%, Is elite: {_ScavSkillManager.FirstAid.IsEliteLevel}");

                return bonus;
            }
        }
    }
}