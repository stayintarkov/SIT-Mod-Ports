using System;
using System.Linq;
using System.Reflection;
using Diz.Binding;
using EFT.UI;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerEncumbranceBar.Utils
{
    public static class UIUtils
    {
        // reflection
        private static FieldInfo _uiElementUiField = AccessTools.Field(typeof(UIElement), "UI");
        private static Type _uiFieldBaseType = _uiElementUiField.FieldType.BaseType;
        private static MethodInfo _uiDisposeMethod = AccessTools.Method(_uiFieldBaseType, "Dispose");
        private static MethodInfo _uiBindEventMethod = _uiFieldBaseType.GetMethods().First(m =>
            {
                return m.Name == "BindEvent" && m.GetParameters().Count() == 2;
            });
        private static MethodInfo _uiAddDisposableMethod =
            AccessTools.Method(_uiFieldBaseType, "AddDisposable", new Type[] { typeof(Action) });

        public static Image CreateImage(string name, Transform parent, Vector2 imageSize, Texture2D texture)
        {
            var imageGO = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            imageGO.transform.SetParent(parent);
            imageGO.transform.localScale = Vector3.one;
            imageGO.GetRectTransform().sizeDelta = imageSize;
            imageGO.GetRectTransform().anchoredPosition = Vector2.zero;

            var image = imageGO.AddComponent<Image>();
            image.sprite = Sprite.Create(texture,
                                         new Rect(0f, 0f, texture.width, texture.height),
                                         new Vector2(texture.width / 2, texture.height / 2));
            image.type = Image.Type.Simple;

            return image;
        }

        public static Image CreateProgressImage(string name, Transform parent, Vector2 barSize, Texture2D texture)
        {
            var image = CreateImage(name, parent, barSize, texture);

            // setup progress bar background
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillAmount = 1;

            return image;
        }

        public static TMP_Text CreateText(GameObject template, string name, Transform parent, Vector2 totalSize, float textFontSize)
        {
            var textGO = GameObject.Instantiate(template);
            textGO.name = name;
            textGO.transform.SetParent(parent);
            textGO.ResetTransform();
            textGO.GetRectTransform().sizeDelta = totalSize;
            textGO.GetRectTransform().anchorMin = new Vector2(0.5f, 0.5f);
            textGO.GetRectTransform().anchorMax = new Vector2(0.5f, 0.5f);
            textGO.GetRectTransform().pivot = new Vector2(0.5f, 0.5f);

            var text = textGO.GetComponent<TMP_Text>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSizeMin = textFontSize;
            text.fontSizeMax = textFontSize;
            text.fontSize = textFontSize;

            return text;
        }

        public static void UIDispose(this UIElement element)
        {
            var ui = _uiElementUiField.GetValue(element);
            _uiDisposeMethod.Invoke(ui, new object[]{});
        }

        public static void UIBindEvent(this UIElement element, BindableEvent bindableEvent, Action action)
        {
            var ui = _uiElementUiField.GetValue(element);
            _uiBindEventMethod.Invoke(ui, new object[]{ bindableEvent, action });
        }

        public static void UIAddDisposable(this UIElement element, Action action)
        {
            var ui = _uiElementUiField.GetValue(element);
            _uiAddDisposableMethod.Invoke(ui, new object[]{ action });
        }
    }
}
