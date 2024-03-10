// #define DEBUG_DETAILS
using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;

namespace ThatsLit
{
    public static class Utility
    {
        public static bool IsBoss (WildSpawnType type) => type switch {
            WildSpawnType.bossBoar => true,
            WildSpawnType.bossBoarSniper => true,
            WildSpawnType.bossBully => true,
            WildSpawnType.bossGluhar => true,
            WildSpawnType.bossKilla => true,
            WildSpawnType.bossKnight => true,
            WildSpawnType.bossKojaniy => true,
            WildSpawnType.bossSanitar => true,
            WildSpawnType.bossTagilla => true,
            WildSpawnType.bossZryachiy => true,
            WildSpawnType.followerBigPipe => true,
            WildSpawnType.followerBirdEye => true,
            WildSpawnType.followerBoar => true,
            WildSpawnType.followerBully => true,
            WildSpawnType.followerKojaniy => true,
            WildSpawnType.followerSanitar => true,
            WildSpawnType.followerTagilla => true,
            WildSpawnType.followerZryachiy => true,
            WildSpawnType.sectantPriest => true,
            _ => false
        };
        public static bool IsBossNerfExcluded (WildSpawnType type) => type switch {
            // WildSpawnType.bossTagilla => true,
            // WildSpawnType.followerTagilla => true,
            _ => false
        };

        internal static float GetInGameDayTime()
        {
            if (Singleton<GameWorld>.Instance?.GameDateTime == null) return 19f;

            var GameDateTime = Singleton<GameWorld>.Instance.GameDateTime.Calculate();

            float minutes = GameDateTime.Minute / 59f;
            return GameDateTime.Hour + minutes;
        }

        internal static IEnumerable<T> FindComponents<T> (Item topLevelItem) where T: class, IItemComponent
        {
            foreach (var it in topLevelItem.GetAllItems())
            {
                yield return it.GetItemComponent<T>();
            }
        }

        static string lastLogged;
        internal static void CalculateDetailScore (string name, int num, out float prone, out float crouch)
        {
            prone = 0;
            crouch = 0;
            if (num == 0)
            {
                prone = 0;
                crouch = 0;
                return;
            }

            if (name.EndsWith("e2eb60")
             || name.EndsWith("df6e82")
             || name.EndsWith("7c58e7")
             || name.EndsWith("994963")
            )
            {
                prone = 0.05f * Mathf.Pow(Mathf.Clamp01(num / 20f), 2) * Mathf.Min(num, 50); // Needs cluster
                crouch = 0.005f * Mathf.Min(num, 20);
            }
            else if (name.EndsWith("27bbce")) // Grass_new_3_D_27bbce, shorter and smaller, cross shape
            {
                prone = 0.008f * num;
                crouch = 0;
            }
            else if (name.EndsWith("fa097b")
                  || name.EndsWith("2adee9")) // low res, bigger yellower grass cross
            {
                prone = 0.06f * num; 
                crouch = 0.01f * num;
            }
            else if (name.EndsWith("eb7931"))
            {
                prone = 0.06f * num; 
                crouch = 0.01f * num;
            }
            else if (name.EndsWith("adb33a"))
            {
                prone = 0.02f * num;
                crouch = 0.02f * num;
            }
            else if (name.EndsWith("f83e15"))
            {
                prone = 0.04f * num;
                crouch = 0.03f * num;
            }
            else if (name.EndsWith("ead4fa"))
            {
                prone = 0.06f * num;
                crouch = 0.008f * num;
            }
            else if (name.EndsWith("40d9d4"))
            {
                prone = 0.007f * num;
                crouch = 0.009f * num;
            }
            else if (name.EndsWith("4ad690")
                || name.EndsWith("bf0a23")
            )
            {
                prone = 0.007f * num;
                crouch = 0.007f * num;
            }
            else if (name.EndsWith("b6cf18"))
            {
                prone = 0.01f * num;
                crouch = 0.01f * num;
            }
            else if (name.EndsWith("a84c21"))
            {
                prone = 0.007f * num;
                crouch = 0.006f * num;
            }

            else if (name.EndsWith("e9cd39")) {} // rock
            else if (ThatsLitPlugin.DebugInfo.Value)
            {
                if (Time.frameCount % 47 == 0 && name != lastLogged)
                {
                    NotificationManagerClass.DisplayWarningNotification(string.Format("That's Lit Debug: Missing terrain detail: {0}", name));
                    lastLogged = name;
                }
            }


                // I REALLY DONT WANT TO CALL SUBSTRING HERE
                // switch (string.Intern(name.Substring(name.Length - 6, 6)))
                // {
                //     case "e2eb60": // Grass_new_1_D_e2eb60, normal grass, 8~12
                //     case "df6e82": // Grass_02_512_df6e82
                //     case "7c58e7": // Grass5_512_D_7c58e7
                //     // case "!vertexlit_rock_e9cd39":
                //     case "994963": // _Grass3_D_994963
                //         prone = 0.05f * Mathf.Pow(Mathf.Clamp01(num / 10f), 2) * num; // Needs cluster
                //         crouch = 0.005f * num;
                //         break;
                //     case "27bbce": // Grass_new_3_D_27bbce, shorter and smaller, cross shape
                //         prone = 0.008f * num;
                //         crouch = 0;
                //         break;
                //     case "fa097b": // Grass_new_2_D_fa097b, denser and slightly bigger grass cluster
                //         prone = 0.06f * num; 
                //         crouch = 0.01f * num;
                //         break;
                //     case "eb7931": // Grass_2_roma_eb7931, brown, dense, somewhat tall
                //         prone = 0.07f * num; 
                //         crouch = 0.02f * num;
                //         break;
                //     case "adb33a": // Grass6_D_adb33a, wheat like
                //         prone = 0.02f * num;
                //         crouch = 0.02f * num;
                //         break;
                //     case "f83e15": // _T_WhitGrass_A_f83e15, tall white grass
                //         prone = 0.04f * num;
                //         crouch = 0.03f * num;
                //         break;
                //     case "ead4fa": // Field_grass_D_ead4fa, with little white flowers
                //         prone = 0.06f * num;
                //         crouch = 0.008f * num;
                //         break;
                //     case "40d9d4": // Grass2_D_40d9d4, thin, tall, wheat
                //         prone = 0.007f * num;
                //         crouch = 0.009f * num;
                //         break;
                //     case "a84c21": // Grass5_D_a84c21, shorter wheat like
                //         prone = 0.007f * num;
                //         crouch = 0.006f * num;
                //         break;
                //     case "4ad690": // grass11_4ad690
                //     case "bf0a23": // Grass4_D_bf0a23, reed like, thin and tall
                //         prone = 0.007f * num;
                //         crouch = 0.007f * num;
                //         break;
                //     case "b6cf18": // _T_KrapivaLittle_A_b6cf18, tall, green
                //         prone = 0.01f * num;
                //         crouch = 0.01f * num;
                //         break;
                //     default:
                //         return;
                // }
            // }
            
        }

        internal static void DetermineShiningEquipments(Player player, out bool vLight, out bool vLaser, out bool irLight, out bool irLaser, out bool vLightSub, out bool vLaserSub, out bool irLightSub, out bool irLaserSub)
        {
            vLight = vLaser = irLight = irLaser = vLightSub = vLaserSub = irLightSub = irLaserSub = false;
            Weapon active = null;
            if (player?.ActiveSlot?.ContainedItem != null)
            {
                Weapon weapon = player.ActiveSlot.ContainedItem as Weapon;
                active = weapon;
                if (weapon != null)
                foreach (var light in FindComponents<LightComponent>(weapon))
                {
                    if (light == null || !light.IsActive) continue;
                    MapComponentsModes(light.Item.TemplateId, light.SelectedMode, out bool thisLight, out bool thisLaser, out bool thisLightIsIR, out bool thisLaserIsIR);
                    if (thisLight && !thisLightIsIR) vLight = true;
                    if (thisLight && thisLightIsIR) irLight = true;
                    if (thisLaser && !thisLaserIsIR) vLaser = true;
                    if (thisLaser && thisLaserIsIR) irLaser = true;
                    if (vLight) return; // Early return
                }
            }

            var inv = player?.ActiveSlot?.ContainedItem?.Owner as InventoryControllerClass;
            if (inv == null) return;

            var helmet = inv?.Inventory?.Equipment?.GetSlot(EquipmentSlot.Headwear)?.ContainedItem;

            if (helmet != null)
            {
                foreach (var light in FindComponents<LightComponent>(helmet))
                {
                    if (light == null || !light.IsActive) continue;
                    MapComponentsModes(light.Item.TemplateId, light.SelectedMode, out bool thisLight, out bool thisLaser, out bool thisLightIsIR, out bool thisLaserIsIR);
                    if (thisLight && !thisLightIsIR) vLight = true;
                    if (thisLight && thisLightIsIR) irLight = true;
                    if (thisLaser && !thisLaserIsIR) vLaser = true;
                    if (thisLaser && thisLaserIsIR) irLaser = true;
                    if (vLight) return; // Early return
                }
            }

            var primary1 = inv?.Inventory?.Equipment?.GetSlot(EquipmentSlot.FirstPrimaryWeapon)?.ContainedItem;
            if (active != primary1 && primary1 != null)
            {
                Weapon weapon = primary1 as Weapon;
                if (weapon != null) 
                foreach (var light in FindComponents<LightComponent>(weapon))
                {
                    if (light == null || !light.IsActive) continue;
                    MapComponentsModes(light.Item.TemplateId, light.SelectedMode, out bool thisLight, out bool thisLaser, out bool thisLightIsIR, out bool thisLaserIsIR);
                    if (thisLight && !thisLightIsIR) vLightSub = true;
                    if (thisLight && thisLightIsIR) irLightSub = true;
                    if (thisLaser && !thisLaserIsIR) vLaserSub = true;
                    if (thisLaser && thisLaserIsIR)  irLaserSub = true;
                }
            }

            var holstered = inv?.Inventory?.Equipment?.GetSlot(EquipmentSlot.Holster)?.ContainedItem;
            if (active != holstered && holstered != null)
            {
                Weapon weapon = holstered as Weapon;
                if (weapon != null)
                foreach (var light in FindComponents<LightComponent>(weapon))
                {
                    if (light == null || !light.IsActive) continue;
                    MapComponentsModes(light.Item.TemplateId, light.SelectedMode, out bool thisLight, out bool thisLaser, out bool thisLightIsIR, out bool thisLaserIsIR);
                    if (thisLight && !thisLightIsIR) vLightSub = true;
                    if (thisLight && thisLightIsIR) irLightSub = true;
                    if (thisLaser && !thisLaserIsIR) vLaserSub = true;
                    if (thisLaser && thisLaserIsIR) irLaserSub = true;
                }
            }

            var secondary = inv?.Inventory?.Equipment?.GetSlot(EquipmentSlot.SecondPrimaryWeapon)?.ContainedItem;
            if (active != secondary && secondary != null)
            {
                Weapon weapon = secondary as Weapon;
                if (weapon != null)
                foreach (var light in FindComponents<LightComponent>(weapon))
                {
                    if (light == null || !light.IsActive) continue;
                    MapComponentsModes(light.Item.TemplateId, light.SelectedMode, out bool thisLight, out bool thisLaser, out bool thisLightIsIR, out bool thisLaserIsIR);
                    if (thisLight && !thisLightIsIR) vLightSub = true;
                    if (thisLight && thisLightIsIR) irLightSub = true;
                    if (thisLaser && !thisLaserIsIR) vLaserSub = true;
                    if (thisLaser && thisLaserIsIR) irLaserSub = true;
                }
            }
            // GClass2550 544909bb4bdc2d6f028b4577 x item tactical_all_insight_anpeq15 2457 / V + IR + IRL / MODES: 4  V -> IR -> IRL -> IR+IRL
            // 560d657b4bdc2da74d8b4572 tactical_all_zenit_2p_kleh_vis_laser MODES: 3, F -> F+V -> V
            // GClass2550 56def37dd2720bec348b456a item tactical_all_surefire_x400_vis_laser 2457 F + V MDOES: 3: F -> F + V -> V
            // 57fd23e32459772d0805bcf1 item tactical_all_holosun_ls321 2457 V + IR + IRL MDOES 4: V -> IR -> IRL -> IRL + IR
            // 55818b164bdc2ddc698b456c tactical_all_zenit_2irs_kleh_lam MODES: 3 IRL -> IRL+IR -> IR
            // 5a7b483fe899ef0016170d15 tactical_all_surefire_xc1 MODES: 1
            // 5a800961159bd4315e3a1657 tactical_all_glock_gl_21_vis_lam MODES 3
            // 5b07dd285acfc4001754240d tactical_all_steiner_las_tac_2 Modes 1

            // "_id": "5b3a337e5acfc4704b4a19a0", "_name": "tactical_all_zenit_2u_kleh", 1
            //"_id": "5c06595c0db834001a66af6c", "_name": "tactical_all_insight_la5", 4, V -> IR -> IRL -> IRL+IR
            //"_id": "5c079ed60db834001a66b372", "_name": "tactical_tt_dlp_tactical_precision_laser_sight", 1
            //"_id": "5c5952732e2216398b5abda2", "_name": "tactical_all_zenit_perst_3", 4
            //"_id": "5cc9c20cd7f00c001336c65d", "_name": "tactical_all_ncstar_tactical_blue_laser", 1
            //"_id": "5d10b49bd7ad1a1a560708b0", "_name": "tactical_all_insight_anpeq2", 2
            //"_id": "5d2369418abbc306c62e0c80", "_name": "tactical_all_steiner_9021_dbal_pl", 6 / F -> V -> F+V -> IRF -> IR -> IRF+IR
            //"_id": "61605d88ffa6e502ac5e7eeb", "_name": "tactical_all_wilcox_raptar_es", 5 / RF -> V -> IR -> IRL -> IRL+IR
            //"_id": "626becf9582c3e319310b837", "_name": "tactical_all_insight_wmx200", 2
            //"_id": "6272370ee4013c5d7e31f418", "_name": "tactical_all_olight_baldr_pro", 3
            //"_id": "6272379924e29f06af4d5ecb", "_name": "tactical_all_olight_baldr_pro_tan", 3


            //"_id": "57d17c5e2459775a5c57d17d", "_name": "flashlight_ultrafire_WF-501B", 1 (2) (different slot)
            //"_id": "59d790f486f77403cb06aec6", "_name": "flashlight_armytek_predator_pro_v3_xhp35_hi", 1(2) (different slot)


            // "_id": "5bffcf7a0db83400232fea79", "_name": "pistolgrip_tt_pm_laser_tt_206", always on
        }
        static void MapComponentsModes(string templateId, int selectedMode, out bool light, out bool laser, out bool lightIsIR, out bool laserIsIR)
        {
            light = laser = laserIsIR = lightIsIR = false;

            switch (templateId)
            {
                case "544909bb4bdc2d6f028b4577": // tactical_all_insight_anpeq15
                case "57fd23e32459772d0805bcf1": // tactical_all_holosun_ls321
                case "5c06595c0db834001a66af6c": // tactical_all_insight_la5
                case "5c5952732e2216398b5abda2": // tactical_all_zenit_perst_3
                    switch (selectedMode)
                    {
                        case 0:
                            laser = true;
                            break;
                        case 1:
                            laser = laserIsIR = true;
                            break;
                        case 2:
                            light = lightIsIR = true;
                            break;
                        case 3:
                            laser = laserIsIR = light = lightIsIR = true;
                            break;
                    }
                    break;
                case "61605d88ffa6e502ac5e7eeb": // tactical_all_wilcox_raptar_es
                    switch (selectedMode)
                    {
                        case 1:
                            laser = true;
                            break;
                        case 2:
                            laser = laserIsIR = true;
                            break;
                        case 3:
                            light = lightIsIR = true;
                            break;
                        case 4:
                            laser = laserIsIR = light = lightIsIR = true;
                            break;
                    }
                    break;
                case "560d657b4bdc2da74d8b4572": // tactical_all_zenit_2p_kleh_vis_laser
                case "56def37dd2720bec348b456a": // tactical_all_surefire_x400_vis_laser
                case "5a800961159bd4315e3a1657": // tactical_all_glock_gl_21_vis_lam
                case "6272379924e29f06af4d5ecb": // tactical_all_olight_baldr_pro_tan
                case "6272370ee4013c5d7e31f418": // tactical_all_olight_baldr_pro
                    switch (selectedMode)
                    {
                        case 0:
                            light = true;
                            break;
                        case 1:
                            laser = light = true;
                            break;
                        case 2:
                            laser = true;
                            break;
                    }
                    break;
                case "55818b164bdc2ddc698b456c": // tactical_all_zenit_2irs_kleh_lam
                    switch (selectedMode)
                    {
                        case 0:
                            light = lightIsIR = true;
                            break;
                        case 1:
                            laser = laserIsIR = light = lightIsIR = true;
                            break;
                        case 2:
                            laser = laserIsIR = true;
                            break;
                    }
                    break;
                case "5a7b483fe899ef0016170d15": // tactical_all_surefire_xc1
                case "5b3a337e5acfc4704b4a19a0": // tactical_all_zenit_2u_kleh
                case "59d790f486f77403cb06aec6": // flashlight_armytek_predator_pro_v3_xhp35_hi
                case "57d17c5e2459775a5c57d17d": // flashlight_ultrafire_WF
                    light = true;
                    break;
                case "5b07dd285acfc4001754240d": // tactical_all_steiner_las_tac_2
                case "5c079ed60db834001a66b372": // tactical_tt_dlp_tactical_precision_laser_sight
                case "5cc9c20cd7f00c001336c65d": // tactical_all_ncstar_tactical_blue_laser
                case "5bffcf7a0db83400232fea79": // pistolgrip_tt_pm_laser_tt_206
                    laser = true;
                    break;
                case "5d10b49bd7ad1a1a560708b0": // tactical_all_insight_anpeq2
                    switch (selectedMode)
                    {
                        case 0:
                            laser = laserIsIR = true;
                            break;
                        case 1:
                            laser = laserIsIR = light = lightIsIR = true;
                            break;
                        case 2:
                            break;
                    }
                    break;
                case "5d2369418abbc306c62e0c80": // tactical_all_steiner_9021_dbal_pl
                    switch (selectedMode)
                    {
                        case 0:
                            light = true;
                            break;
                        case 1:
                            laser = true;
                            break;
                        case 2:
                            laser = light = true;
                            break;
                        case 3:
                            light = lightIsIR = true;
                            break;
                        case 4:
                            laser = laserIsIR = true;
                            break;
                        case 5:
                            light = lightIsIR = laser = laserIsIR = true;
                            break;
                    }
                    break;
                case "626becf9582c3e319310b837": // tactical_all_insight_wmx200
                    switch (selectedMode)
                    {
                        case 0:
                            light = true;
                            break;
                        case 1:
                            light = lightIsIR = true;
                            break;
                    }
                    break;
                case "644a3df63b0b6f03e101e065": // MAWL-C1+
                    switch (selectedMode)
                    {
                        case 0:
                            light = true;
                            break;
                        case 1:
                            light = true;
                            break;
                        case 2:
                            lightIsIR = true;
                            break;
                        case 3:
                            lightIsIR = true;
                            break;
                        case 4:
                            laserIsIR = true;
                            break;
                        case 5:
                            laserIsIR = true;
                            break;
                    }
                    break;
                default:
                    if (CustomLightAndLaser == null) return;
                    if (CustomLightAndLaser.TryGetValue((templateId, selectedMode), out var setup))
                    {
                        light = setup.light;
                        lightIsIR = setup.lightIsIR;
                        laser = setup.laser;
                        laserIsIR = setup.laserIsIR;
                    }
                    break;
            }
        }

        static Dictionary<(string, int), (bool light, bool lightIsIR, bool laser, bool laserIsIR)> CustomLightAndLaser { get; set; }
        public static void RegisterCustomLightAndLaser (string templateId, int mode, bool light, bool lightIsIR, bool laser, bool laserIsIR)
        {
            if (CustomLightAndLaser == null) CustomLightAndLaser = new Dictionary<(string, int), (bool light, bool lightIsIR, bool laser, bool laserIsIR)>();
            CustomLightAndLaser.Add((templateId, mode), (light, lightIsIR, laser, laserIsIR));
        }
        static HashSet<string> CustomNightVisionScopes { get; set; }
        static Dictionary<string, float> CustomThermalScopes { get; set; }
        public static void RegisterCustomNightVisionScopes (string templateId)
        {
            if (CustomNightVisionScopes == null) CustomNightVisionScopes = new HashSet<string>();
            CustomNightVisionScopes.Add(templateId);
        }
        public static void RegisterCustomThermalScopes (string templateId, float effDis)
        {
            if (CustomThermalScopes == null) CustomThermalScopes = new Dictionary<string, float>();
            CustomThermalScopes.Add(templateId, effDis);
        }
// void RegisterExample ()
// {
//     Type type = Type.GetType("ThatsLit.Utility, ThatsLit");
//     if (type == null)
//         return;
//     var m = type.GetMethod("RegisterCustomLightAndLaser"); // or RegisterCustomNightVisionScopes, RegisterCustomThermalScopes
//     if (m == null)
//     {
//         LogWarning("ThatsLit endpoint not found.");
//         return;
//     }
//     m.Invoke(null, new object[] { templateId, mode, light, lightIsIR, laser, laserIsIR });
// }

        internal static bool IsNightVisionScope (string templateId)
        {
            return templateId switch
            {
                "5b3b6e495acfc4330140bd88" => true,
                "5a7c74b3e899ef0014332c29" => true,
                _ => CustomNightVisionScopes == null? false : CustomNightVisionScopes.Contains(templateId)
            };
    // "_id": "5b3b6e495acfc4330140bd88",
    // "_name": "scope_base_armasight_vulcan_gen3_bravo_mg_3,5x",
    
    // "_id": "5a7c74b3e899ef0014332c29",
    // "_name": "scope_dovetail_npz_nspum_3,5x",
        }
        internal static bool IsThermalScope (string templateId, out float effDis)
        {
            effDis = 200;
            switch (templateId)
            {
                case "6478641c19d732620e045e17":
                    return true;
                case "63fc44e2429a8a166c7f61e6":
                    return true;
                case "5a1eaa87fcdbcb001865f75e":
                    return true;
                case "5d1b5e94d7ad1a2b865a96b0":
                    return true;
                case "606f2696f2cb2e02a42aceb1":
                    effDis = 5;
                    return true;
                case "609bab8b455afd752b2e6138":
                    return true;
                default:
                    return CustomThermalScopes == null? false : CustomThermalScopes.TryGetValue(templateId, out effDis);

            }
            // THERMAL

            // "_id": "6478641c19d732620e045e17",
            // "_name": "scope_all_sig_sauer_echo1_thermal_reflex_sight_1_2x_30hz",

            // "_id": "63fc44e2429a8a166c7f61e6",
            // "_name": "scope_base_armasight_zeus_pro_640_2_16x50_30hz",
            
            // "_id": "5a1eaa87fcdbcb001865f75e",
            // "_name": "scope_base_trijicon_reap-ir",
            
            // "_id": "5d1b5e94d7ad1a2b865a96b0",
            // "_name": "scope_all_flir_rs32_225_9x_35_60hz",

            // "_id": "606f2696f2cb2e02a42aceb1",
            // "_name": "tactical_mp155_kalashnikov_ultima_camera",

            // "_id": "609bab8b455afd752b2e6138",
            // "_name": "scope_all_torrey_pines_logic_t12_w_30hz",
        }

        internal static void GUILayoutDrawAsymetricMeter(int level)
        {
            if (level < -10)
            {
                GUILayout.Label("  ▰▰▰▰▰▰▰▰▰▰ ▱▱▱▱▱▱▱▱▱▱");
                return;
            }
            if (level > 10)
            {
                GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▰▰▰▰▰");
                return;
            }
            switch (level)
            {
                case -11:
                    GUILayout.Label("  ▰▰▰▰▰▰▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱");
                    break;
                case -10:
                    GUILayout.Label("  ▰▰▰▰▰▰▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱");
                    break;
                case -9:
                    GUILayout.Label("  ▱▰▰▰▰▰▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱");
                    break;
                case -8:
                    GUILayout.Label("  ▱▱▰▰▰▰▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱");
                    break;
                case -7:
                    GUILayout.Label("  ▱▱▱▰▰▰▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱");
                    break;
                case -6:
                    GUILayout.Label("  ▱▱▱▱▰▰▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱");
                    break;
                case -5:
                    GUILayout.Label("  ▱▱▱▱▱▰▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱");
                    break;
                case -4:
                    GUILayout.Label("  ▱▱▱▱▱▱▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱");
                    break;
                case -3:
                    GUILayout.Label("  ▱▱▱▱▱▱▱▰▰▰  ▱▱▱▱▱▱▱▱▱▱");
                    break;
                case -2:
                    GUILayout.Label("  ▱▱▱▱▱▱▱▱▰▰  ▱▱▱▱▱▱▱▱▱▱");
                    break;
                case -1:
                    GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▰  ▱▱▱▱▱▱▱▱▱▱");
                    break;
                case 0:
                    GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▱▱▱▱▱▱▱▱▱▱");
                    break;
                case 1:
                    GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▱▱▱▱▱▱▱▱▱");
                    break;
                case 2:
                    GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▱▱▱▱▱▱▱▱");
                    break;
                case 3:
                    GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▱▱▱▱▱▱▱");
                    break;
                case 4:
                    GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▱▱▱▱▱▱");
                    break;
                case 5:
                    GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▱▱▱▱▱");
                    break;
                case 6:
                    GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▰▱▱▱▱");
                    break;
                case 7:
                    GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▰▰▱▱▱");
                    break;
                case 8:
                    GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▰▰▰▱▱");
                    break;
                case 9:
                    GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▰▰▰▰▱");
                    break;
                case 10:
                    GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▰▰▰▰▰");
                    break;
                case 11:
                    GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▰▰▰▰▰");
                    break;
            }
        }

        internal static void RightAlignedGUILabel (string str)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(str);
            }
        }

        internal static void GUILayoutFoliageMeter(int level)
        {
            if (level <= 0)
            {
                GUILayout.Label("  FOLIAGE  ▱▱▱▱▱▱▱▱▱▱");
                return;
            }
            if (level >= 10)
            {
                GUILayout.Label("  FOLIAGE  ▰▰▰▰▰▰▰▰▰▰");
                return;
            }
            switch (level)
            {
                case 1:
                    GUILayout.Label("  FOLIAGE  ▰▱▱▱▱▱▱▱▱▱");
                    break;
                case 2:
                    GUILayout.Label("  FOLIAGE  ▰▰▱▱▱▱▱▱▱▱");
                    break;
                case 3:
                    GUILayout.Label("  FOLIAGE  ▰▰▰▱▱▱▱▱▱▱");
                    break;
                case 4:
                    GUILayout.Label("  FOLIAGE  ▰▰▰▰▱▱▱▱▱▱");
                    break;
                case 5:
                    GUILayout.Label("  FOLIAGE  ▰▰▰▰▰▱▱▱▱▱");
                    break;
                case 6:
                    GUILayout.Label("  FOLIAGE  ▰▰▰▰▰▰▱▱▱▱");
                    break;
                case 7:
                    GUILayout.Label("  FOLIAGE  ▰▰▰▰▰▰▰▱▱▱");
                    break;
                case 8:
                    GUILayout.Label("  FOLIAGE  ▰▰▰▰▰▰▰▰▱▱");
                    break;
                case 9:
                    GUILayout.Label("  FOLIAGE  ▰▰▰▰▰▰▰▰▰▱");
                    break;
            }
        }

        internal static void GUILayoutTerrainMeter(int level)
        {
            if (level <= 0)
            {
                GUILayout.Label("  TERRAIN  ▱▱▱▱▱▱▱▱▱▱");
                return;
            }
            if (level >= 10)
            {
                GUILayout.Label("  TERRAIN  ▰▰▰▰▰▰▰▰▰▰");
                return;
            }
            switch (level)
            {
                case 1:
                    GUILayout.Label("  TERRAIN  ▰▱▱▱▱▱▱▱▱▱");
                    break;
                case 2:
                    GUILayout.Label("  TERRAIN  ▰▰▱▱▱▱▱▱▱▱");
                    break;
                case 3:
                    GUILayout.Label("  TERRAIN  ▰▰▰▱▱▱▱▱▱▱");
                    break;
                case 4:
                    GUILayout.Label("  TERRAIN  ▰▰▰▰▱▱▱▱▱▱");
                    break;
                case 5:
                    GUILayout.Label("  TERRAIN  ▰▰▰▰▰▱▱▱▱▱");
                    break;
                case 6:
                    GUILayout.Label("  TERRAIN  ▰▰▰▰▰▰▱▱▱▱");
                    break;
                case 7:
                    GUILayout.Label("  TERRAIN  ▰▰▰▰▰▰▰▱▱▱");
                    break;
                case 8:
                    GUILayout.Label("  TERRAIN  ▰▰▰▰▰▰▰▰▱▱");
                    break;
                case 9:
                    GUILayout.Label("  TERRAIN  ▰▰▰▰▰▰▰▰▰▱");
                    break;
            }
        }

        public static string DetermineDir (Vector3 dir)
        {
            var dirFlat = (new Vector2 (dir.x, dir.z)).normalized;
            var angle = Vector2.SignedAngle(Vector2.up, dirFlat);
            if (angle >= -22.5f && angle <= 22.5f)
            {
                return "N";
            }
            else if (angle >= 22.5f && angle <= 67.5f)
            {
                return "NE";
            }
            else if (angle >= 67.5f && angle <= 112.5f)
            {
                return "E";
            }
            else if (angle >= 112.5f && angle <= 157.5f)
            {
                return "SE";
            }
            else if (angle >= 157.5f && angle <= 180f || angle >= -180f && angle <= -157.5f)
            {
                return "S";
            }
            else if (angle >= -157.5f && angle <= -112.5f)
            {
                return "SW";
            }
            else if (angle >= -112.5f && angle <= -67.5f)
            {
                return "W";
            }
            else if (angle >= -67.5f && angle <= -22.5f)
            {
                return "NW";
            }
            else return "?";
        }
    }
}