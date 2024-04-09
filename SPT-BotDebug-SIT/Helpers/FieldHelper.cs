using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace DrakiaXYZ.BotDebug.Helpers
{
    public static class FieldHelper
    {
        public static FieldInfo PlayerOwnerField = AccessTools.Field(typeof(ActorDataStruct), "PlayerOwner");
        public static FieldInfo BotDataField = AccessTools.Field(typeof(ActorDataStruct), "BotData");
        public static FieldInfo HealthDataField = AccessTools.Field(typeof(ActorDataStruct), "HeathsData");

        public static Dictionary<string, FieldInfo> Fields = new Dictionary<string, FieldInfo>();
        public static Dictionary<string, PropertyInfo> Properties = new Dictionary<string, PropertyInfo>();
        public static T Field<T>(object instance, string fieldName)
        {
            if (!Fields.TryGetValue(fieldName, out var fieldInfo))
            {
                fieldInfo = AccessTools.Field(instance.GetType(), fieldName);
                Fields.Add(fieldName, fieldInfo);
            }

            return (T)fieldInfo.GetValue(instance);
        }

        public static T Property<T>(object instance, string propertyName)
        {
            if (!Properties.TryGetValue(propertyName, out var propertyInfo))
            {
                propertyInfo = AccessTools.Property(instance.GetType(), propertyName);
                Properties.Add(propertyName, propertyInfo);
            }

            return (T)propertyInfo.GetValue(instance);
        }
    }
}