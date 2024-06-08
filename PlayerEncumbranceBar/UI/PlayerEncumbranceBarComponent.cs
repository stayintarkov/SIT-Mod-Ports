using System;
using System.IO;
using System.Reflection;
using Comfort.Common;
using DG.Tweening;
using EFT.HealthSystem;
using EFT.UI;
using EFT.UI.Health;
using HarmonyLib;
using PlayerEncumbranceBar.Config;
using PlayerEncumbranceBar.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerEncumbranceBar
{
    public class PlayerEncumbranceBarComponent : UIElement
    {
        private static string _progressTexturePath = Path.Combine(Plugin.PluginFolder, "progressBarFill.png"); // 1x9
        private static FieldInfo _healthParameterPanelCurrentValueField = AccessTools.Field(typeof(HealthParameterPanel), "_currentValue");

        private static IHealthController _healthController;  // set by AttachToHealthParametersPanel
        private static GameObject _textTemplate;
        private static Color _unencumberedColor = new Color(0.6431f, 0.7725f, 0.6627f, 1f);
        private static Color _overweightColor = new Color(0.9176f, 0.7098f, 0.1961f, 1f);
        private static Color _completelyOverweightColor = new Color(0.7686f, 0f, 0f, 1f);
        private static Color _walkingDrainsColor; // set by lerp
        private static Vector2 _position = new Vector2(0, -25);
        private static Vector2 _barSize = new Vector2(550, 9);
        private static Vector2 _tickSize = new Vector2(1, 9);
        private static Vector2 _textSize = new Vector2(40, 20);
        private static float _textFontSize = 10;
        private static Vector2 _textPosition = new Vector2(0, -11);
        private static float _tweenLength = 0.25f; // seconds

        private Image _progressImage;
        private Image _backgroundImage;
        private Vector2 _baseOverweightLimits;
        private Vector2 _walkOverweightLimits;
        private Image _overweightTickMark;
        private Image _walkingDrainsTickMark;
        private TMP_Text _overweightText;
        private TMP_Text _walkingDrainsText;

        public static PlayerEncumbranceBarComponent AttachToHealthParametersPanel(HealthParametersPanel healthParametersPanel,
                                                                                  HealthParameterPanel weightPanel,
                                                                                  IHealthController healthController)
        {
            // check if healthParametersPanel not yet setup all the way
            if (!healthParametersPanel || !weightPanel)
            {
                return null;
            }

            _healthController = healthController;

            // setup walking drains color, just do halfway between the two other colors
            _walkingDrainsColor = Color.Lerp(_overweightColor, _completelyOverweightColor, 0.5f);

            // get text template
            _textTemplate = (_healthParameterPanelCurrentValueField.GetValue(weightPanel) as TMP_Text).gameObject;

            // setup container
            var containerGO = new GameObject("PlayerEncumbranceBarContainer", typeof(RectTransform));
            containerGO.layer = healthParametersPanel.gameObject.layer;
            containerGO.transform.SetParent(healthParametersPanel.gameObject.transform);
            containerGO.transform.localScale = Vector3.one;
            containerGO.GetRectTransform().sizeDelta = _barSize;
            containerGO.GetRectTransform().anchoredPosition = _position;

            var layoutElement = containerGO.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            // HACK: move HealthParametersPanel to make sure the bottom bar renders behind us
            healthParametersPanel.gameObject.transform.SetAsLastSibling();

            var component = containerGO.AddComponent<PlayerEncumbranceBarComponent>();
            return component;
        }

        private void Awake()
        {
            var texture = TextureUtils.LoadTexture2DFromPath(_progressTexturePath);

            // setup background
            _backgroundImage = UIUtils.CreateProgressImage("Background", transform, _barSize, texture);
            _backgroundImage.color = Color.black;

            // setup current weight
            _progressImage = UIUtils.CreateProgressImage("CurrentWeight", transform, _barSize, texture);

            // setup tick marks
            _overweightTickMark = UIUtils.CreateImage("Overweight Tick", transform, _tickSize, texture);
            _walkingDrainsTickMark = UIUtils.CreateImage("Walking Drains Tick", transform, _tickSize, texture);

            // setup texts
            _overweightText = UIUtils.CreateText(_textTemplate, "Overweight Text", transform, _textSize, _textFontSize);
            _overweightText.GetRectTransform().anchoredPosition = _textPosition;
            _overweightText.color = Color.grey;

            _walkingDrainsText = UIUtils.CreateText(_textTemplate, "Walking Drains Text", transform, _textSize, _textFontSize);
            _walkingDrainsText.GetRectTransform().anchoredPosition = _textPosition;
            _walkingDrainsText.color = Color.grey;

            ShowHideText();
        }

        internal void Show(IHealthController healthController)
        {
            this.UIDispose();

            _healthController = healthController;

            // NOTE: BindEvent seems to call the method immediately
            this.UIBindEvent(GameUtils.Skills.Strength.OnLevelUp, OnStrengthLevelUp);
            this.UIBindEvent(GameUtils.Inventory.OnWeightUpdated, OnUpdateWeight);

            // for health effects
            _healthController.EffectStartedEvent += OnHealthEffect;
			_healthController.EffectResidualEvent += OnHealthEffect;
			_healthController.EffectRemovedEvent += OnHealthEffect;
            this.UIAddDisposable(new Action(() => _healthController.EffectStartedEvent -= OnHealthEffect));
            this.UIAddDisposable(new Action(() => _healthController.EffectResidualEvent -= OnHealthEffect));
            this.UIAddDisposable(new Action(() => _healthController.EffectRemovedEvent -= OnHealthEffect));

            // this might be redundant, with BindEvent calling immediately, but make sure
            // don't tween the initial
            UpdateWeightLimits(false);
        }

        private void MoveRelativeToBar(Component component, float relPos)
        {
            var x = Mathf.Lerp(-_barSize.x/2, _barSize.x / 2, relPos);
            var y = component.GetRectTransform().anchoredPosition.y;
            component.GetRectTransform().anchoredPosition = new Vector2(x, y);
        }

        private void ShowHideText()
        {
            if (Settings.DisplayText.Value)
            {
                _overweightText.gameObject.SetActive(true);
                _walkingDrainsText.gameObject.SetActive(true);
            }
            else
            {
                _overweightText.gameObject.SetActive(false);
                _walkingDrainsText.gameObject.SetActive(false);
            }
        }

        internal void OnSettingChanged()
        {
            ShowHideText();
        }

        private void OnUpdateWeight()
        {
            UpdateWeight();
        }

        private void OnStrengthLevelUp()
        {
            UpdateWeightLimits();
        }

        private void OnHealthEffect(IEffect _)
        {
            UpdateWeightLimits();
        }

        private void UpdateWeightLimits(bool shouldTween = true)
        {
            var relativeModifier = GameUtils.Skills.CarryingWeightRelativeModifier * _healthController.CarryingWeightRelativeModifier;
            var absoluteModifier = _healthController.CarryingWeightAbsoluteModifier * Vector2.one;

            _baseOverweightLimits = GameUtils.GetBaseOverweightLimits() * relativeModifier + absoluteModifier;
            _walkOverweightLimits = GameUtils.GetWalkOverweightLimits() * relativeModifier + absoluteModifier;

            // update tick mark positions
            MoveRelativeToBar(_overweightTickMark, _baseOverweightLimits.x / _baseOverweightLimits.y);
            MoveRelativeToBar(_walkingDrainsTickMark, _walkOverweightLimits.x / _baseOverweightLimits.y);

            // update text values and positions
            MoveRelativeToBar(_overweightText, _baseOverweightLimits.x / _baseOverweightLimits.y);
            MoveRelativeToBar(_walkingDrainsText, _walkOverweightLimits.x / _baseOverweightLimits.y);
            _overweightText.text = $"{_baseOverweightLimits.x:f1}";
            _walkingDrainsText.text = $"{_walkOverweightLimits.x:f1}";

            UpdateWeight(shouldTween);
        }

        private void UpdateWeight(bool shouldTween = true)
        {
            var weight = GameUtils.GetPlayerCurrentWeight();
            var overweightLimit = _baseOverweightLimits.x;
            var walkingDrainsWeightLimit = _walkOverweightLimits.x;
            var maxWeightLimit = _baseOverweightLimits.y;

            // reset colors to unencumbered
            _progressImage.color = _unencumberedColor;
            _overweightTickMark.color = _overweightColor;
            _walkingDrainsTickMark.color = _walkingDrainsColor;
            _overweightText.color = Color.grey;
            _walkingDrainsText.color = Color.grey;

            // overweight colors
            if (weight > overweightLimit)
            {
                _progressImage.color = _overweightColor;
                _overweightTickMark.color = Color.black;
                _overweightText.color = _overweightColor;
            }

            // walking drains color
            if (weight > walkingDrainsWeightLimit)
            {
                _progressImage.color = _walkingDrainsColor;
                _walkingDrainsTickMark.color = Color.black;
                _walkingDrainsText.color = _walkingDrainsColor;
            }

            // max weight color
            if (weight > maxWeightLimit)
            {
                _progressImage.color = _completelyOverweightColor;
            }

            // tween to the proper length or set if not tweening
            if (shouldTween && _tweenLength >= 0)
            {
                _progressImage.DOFillAmount(weight / maxWeightLimit, _tweenLength);
            }
            else
            {
                _progressImage.fillAmount = weight / maxWeightLimit;
            }
        }
    }
}
