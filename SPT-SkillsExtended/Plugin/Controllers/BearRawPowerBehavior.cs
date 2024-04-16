using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
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

        private SkillManager _skillManager => Utils.SetActiveSkillManager();

        private float _hpBonus => _skillManager.BearRawpower.IsEliteLevel
                ? _skillManager.BearRawpower.Level * BEAR_POWER_HP_BONUS + BEAR_POWER_HP_BONUS_ELITE
                : _skillManager.BearRawpower.Level * BEAR_POWER_HP_BONUS;

        private float _carryWeightBonus => _skillManager.BearRawpower.IsEliteLevel
                ? _skillManager.BearRawpower.Level * BEAR_POWER_CARRY_BONUS + BEAR_POWER_CARRY_BONUS_ELITE
                : _skillManager.BearRawpower.Level * BEAR_POWER_CARRY_BONUS;

        private Dictionary<EBodyPart, Profile.ProfileHealth.GClass1768> _origHealthVals = new Dictionary<EBodyPart, Profile.ProfileHealth.GClass1768>();

        private DateTime _lastXpTime = DateTime.Now;

        private int _lastHealthAppliedLevel = -1;
        private int _lastWeightAppliedLevel = -1;

        private void Awake()
        {

        }

        private void Update()
        {
            if (_skillManager == null) { return; }

            ApplyHealthBonus();

            if (Singleton<GameWorld>.Instance?.MainPlayer == null) { return; }

            ApplyXp();
            ApplyWeightBonus();
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

        private void ApplyWeightBonus()
        {
            if (_lastWeightAppliedLevel == _skillManager.BearRawpower.Level) { return; }

            if (!Singleton<BackEndConfig>.Instantiated) { return; }

            BackendConfigSettingsClass bcs = Singleton<BackendConfigSettingsClass>.Instance;

            bcs.Stamina.BaseOverweightLimits = new Vector2(26f * (1f + _carryWeightBonus), 67f * (1 + _carryWeightBonus));
            bcs.Stamina.WalkOverweightLimits = new Vector2(26f * (1f + _carryWeightBonus), 67f * (1 + _carryWeightBonus));
            bcs.Stamina.WalkSpeedOverweightLimits = new Vector2(45f * (1f + _carryWeightBonus), 75f * (1 + _carryWeightBonus));
            bcs.Stamina.SprintOverweightLimits = new Vector2(26f * (1f + _carryWeightBonus), 63f * (1 + _carryWeightBonus));

            if (Singleton<GameWorld>.Instance.MainPlayer != null)
            {
                _player.Physical.BaseOverweightLimits = new Vector2(26f * (1f + _carryWeightBonus), 67f * (1 + _carryWeightBonus));
                _player.Physical.WalkOverweightLimits = new Vector2(26f * (1f +_carryWeightBonus), 67f * (1 + _carryWeightBonus));
                _player.Physical.WalkSpeedOverweightLimits = new Vector2(45f * (1f + _carryWeightBonus), 75f * (1 + _carryWeightBonus));
                _player.Physical.SprintOverweightLimits = new Vector2(26f * (1f + _carryWeightBonus), 63f * (1 + _carryWeightBonus));
            }

            _lastHealthAppliedLevel = _skillManager.BearRawpower.Level;
        }
    }
}
