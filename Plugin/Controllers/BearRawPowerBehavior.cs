using Comfort.Common;
using EFT;
using SkillsExtended.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static SkillsExtended.Helpers.Constants;

namespace SkillsExtended.Controllers
{
    public class BearRawPowerBehavior : MonoBehaviour
    {
        private GameWorld _gameWorld { get => Singleton<GameWorld>.Instance; }

        private Player _player { get => _gameWorld.MainPlayer; }

        private SkillManager _skillManager;

        private float _hpBonus => _skillManager.BearRawpower.IsEliteLevel
                ? _skillManager.BearRawpower.Level * BEAR_POWER_HP_BONUS + BEAR_POWER_HP_BONUS_ELITE
                : _skillManager.BearRawpower.Level * BEAR_POWER_HP_BONUS;

        private float _carryWeightBonus => _skillManager.BearRawpower.IsEliteLevel
                ? _skillManager.BearRawpower.Level * BEAR_POWER_CARRY_BONUS + BEAR_POWER_CARRY_BONUS_ELITE
                : _skillManager.BearRawpower.Level * BEAR_POWER_CARRY_BONUS;

        private Dictionary<EBodyPart, Profile.GClass1622.GClass1624> _origHealthVals = new Dictionary<EBodyPart, Profile.GClass1622.GClass1624>();

        private DateTime _lastXpTime = DateTime.Now;

        private int _lastAppliedLevel = -1;

        private void Awake()
        {

        }

        private void Update()
        {
            SetupSkillManager();

            if (_skillManager == null) { return; }

            ApplyHealthBonus();

            if (Singleton<GameWorld>.Instance?.MainPlayer == null) { return; }

            ApplyXp();           
        }

        private void SetupSkillManager()
        {
            if (_skillManager == null)
            {
                _skillManager = Utils.SetActiveSkillManager();
            }
        }

        private void ApplyXp()
        {
            if (!CanGainXP()) { return; }

            if (_player.Physical.Sprinting && _player.Physical.Overweight > 0f)
            {
                var xpToGain = Mathf.Clamp(_player.Physical.Overweight * 100f, 0f, 1f);

                Plugin.Log.LogDebug($"XP Gained {xpToGain}");

                _player.Skills.BearRawpower.Current += xpToGain;
            }
        }

        private bool CanGainXP()
        {
            TimeSpan elapsed = DateTime.Now - _lastXpTime;

            if (elapsed.TotalSeconds >= BEAR_POWER_UPDATE_TIME)
            {
                _lastXpTime = DateTime.Now;
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ApplyHealthBonus()
        {
            if (_lastAppliedLevel == _skillManager.BearRawpower.Level) { return; }

            var bodyParts = Plugin.Session.Profile.Health.BodyParts;

            foreach (var bodyPart in bodyParts)
            {
                bodyPart.Deconstruct(out EBodyPart key, out Profile.GClass1622.GClass1624 value);

                if (!_origHealthVals.ContainsKey(key))
                {
                    _origHealthVals.Add(key, value);
                }
             
                value.Health.Maximum = Mathf.FloorToInt(_origHealthVals[key].Health.Maximum * (1 + _hpBonus));
            }

            _lastAppliedLevel = _skillManager.BearRawpower.Level;
        }

        private void ApplyWeightBonus()
        {

        }
    }
}
