using System;
using System.Reflection;
using System.Linq;
using EFT;
using SIT.Tarkov.Core;
using SIT.Core.Misc;

namespace AmandsHitmarker
{
    public class AmandsHitmarkerHelper
    {
        private static Type LocalizedType;
        private static MethodInfo LocalizedMethod;

        private static Type RoleType;
        private static MethodInfo GetScavRoleKeyMethod;
        private static MethodInfo IsFollowerMethod;
        private static MethodInfo CountAsBossMethod;
        private static MethodInfo IsBossMethod;

        private static Type TransliterateType;
        private static MethodInfo TransliterateMethod;

        public static bool UsesFSR2UpscalerMethodFound;
        private static MethodInfo UsesFSR2UpscalerMethod;

        public static void Init()
        {
            LocalizedType = PatchConstants.EftTypes.Single((Type x) => x.GetMethod("ParseLocalization") != null);
            LocalizedMethod = LocalizedType.GetMethods().First((MethodInfo x) => x.Name == "Localized" && x.GetParameters().Length == 2 && x.GetParameters()[0].ParameterType == typeof(string) && x.GetParameters()[1].ParameterType == typeof(EStringCase));

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
            RoleType = PatchConstants.EftTypes.Single((Type x) => x.GetMethod("IsBoss", flags) != null && x.GetMethod("Init", flags) != null);

            IsBossMethod = RoleType.GetMethod("IsBoss", flags);
            IsFollowerMethod = RoleType.GetMethod("IsFollower", flags);
            CountAsBossMethod = RoleType.GetMethod("CountAsBoss", flags);
            GetScavRoleKeyMethod = RoleType.GetMethod("GetScavRoleKey", flags);


            TransliterateType = PatchConstants.EftTypes.Single(x => x.GetMethods().Any(t => t.Name == "Transliterate"));
            TransliterateMethod = TransliterateType.GetMethods().Single(x => x.Name == "Transliterate" && x.GetParameters().Length == 1);
            UsesFSR2UpscalerMethod = typeof(SSAA).GetMethod("UsesFSR2Upscaler");
            UsesFSR2UpscalerMethodFound = UsesFSR2UpscalerMethod != null;
        }
        public static string Localized(string id, EStringCase @case)
        {
            return (string)LocalizedMethod.Invoke(null, new object[]
            {
                id,
                @case
            });
        }
        public static bool IsBoss(WildSpawnType role)
        {
            return (bool)IsBossMethod.Invoke(null, new object[]
            {
                role
            });
        }
        public static bool IsFollower(WildSpawnType role)
        {
            return (bool)IsFollowerMethod.Invoke(null, new object[]
            {
                role
            });
        }
        public static bool CountAsBoss(WildSpawnType role)
        {
            return (bool)CountAsBossMethod.Invoke(null, new object[]
            {
                role
            });
        }
        public static string GetScavRoleKey(WildSpawnType role)
        {
            return (string)GetScavRoleKeyMethod.Invoke(null, new object[]
            {
                role
            });
        }
        public static string Transliterate(string text)
        {
            return (string)TransliterateMethod.Invoke(null, new object[]
            {
                text
            });
        }
        public static bool UsesFSR2Upscaler()
        {
            return UsesFSR2UpscalerMethodFound ? (bool)UsesFSR2UpscalerMethod.Invoke(null, new object[]
            {
            }) : false;
        }
    }
}
