using Comfort.Common;
using EFT;
using SkillsExtended.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillsExtended.Controllers
{
    public class BearRawPowerBehavior : MonoBehaviour
    {
        private GameWorld _gameWorld { get => Singleton<GameWorld>.Instance; }

        private Player _player { get => _gameWorld.MainPlayer; }

        private SkillManager _skillManager => Utils.GetActiveSkillManager();

        private float _hpBonus => _skillManager.BearRawpower.IsEliteLevel
                ? _skillManager.BearRawpower.Level * Plugin.SkillData.BearRawPowerSkill.HPBonus + Plugin.SkillData.BearRawPowerSkill.HPBonusElite
                : _skillManager.BearRawpower.Level * Plugin.SkillData.BearRawPowerSkill.HPBonus;

        private Dictionary<EBodyPart, Profile.ProfileHealth.GClass1768> _origHealthVals = [];

        private DateTime _lastXpTime = DateTime.Now;

        private int _lastHealthAppliedLevel = -1;

        private void Awake()
        {
        }

        private void Update()
        {
            if (_skillManager == null) { return; }

            ApplyHealthBonus();

            if (Singleton<GameWorld>.Instance?.MainPlayer == null) { return; }

            ApplyXp();
        }

        private void ApplyXp()
        {
            if (!CanGainXP()) { return; }

            if (_player.Physical.Sprinting && _player.Physical.Overweight > 0f)
            {
                var xpToGain = Mathf.Clamp(_player.Physical.Overweight * 70f, 0f, 1.2f);

                Plugin.Log.LogDebug($"XP Gained {xpToGain}");

                _player.Skills.BearRawpower.Current += xpToGain;
            }
        }

        private bool CanGainXP()
        {
            TimeSpan elapsed = DateTime.Now - _lastXpTime;

            if (elapsed.TotalSeconds >= Plugin.SkillData.BearRawPowerSkill.UpdateTime)
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
            if (_lastHealthAppliedLevel == _skillManager.BearRawpower.Level) { return; }

            var bodyParts = Plugin.Session.Profile.Health.BodyParts;

            foreach (var bodyPart in bodyParts)
            {
                bodyPart.Deconstruct(out EBodyPart key, out Profile.ProfileHealth.GClass1768 value);

                if (!_origHealthVals.ContainsKey(key))
                {
                    _origHealthVals.Add(key, value);
                }

                var bonusHp = Mathf.FloorToInt(_origHealthVals[key].Health.Maximum * (1 + _hpBonus));

                value.Health.Maximum = bonusHp;

                if (!_gameWorld && value.Health.Current != bonusHp)
                {
                    value.Health.Current = bonusHp;
                }
            }

            _lastHealthAppliedLevel = _skillManager.BearRawpower.Level;
        }
    }
}