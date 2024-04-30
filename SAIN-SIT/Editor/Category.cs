using SAIN.Attributes;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.Editor
{
    public sealed class Category
    {
        public Category(AttributesInfoClass attributes)
        {
            CategoryInfo = attributes;
            foreach (FieldInfo field in attributes.ValueType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var attInfo = AttributesGUI.GetAttributeInfo(field);
                if (attInfo != null && !attInfo.Hidden)
                {
                    FieldAttributesList.Add(attInfo);
                }
            }
        }

        public object GetValue(object obj)
        {
            return CategoryInfo.GetValue(obj);
        }

        public void SetValue(object obj, object value)
        {
            CategoryInfo.SetValue(obj, value);
        }

        public readonly AttributesInfoClass CategoryInfo;

        public readonly List<AttributesInfoClass> FieldAttributesList = new List<AttributesInfoClass>();
        public readonly List<AttributesInfoClass> SelectedList = new List<AttributesInfoClass>();

        public bool Open = false;
        public Vector2 Scroll = Vector2.zero;

        public int OptionCount(out int realCount)
        {
            realCount = 0;
            int count = 0;
            foreach (var option in FieldAttributesList)
            {
                if (!option.DoNotShowGUI)
                {
                    count++;
                }
                realCount++;
            }
            return count;
        }
    }
}