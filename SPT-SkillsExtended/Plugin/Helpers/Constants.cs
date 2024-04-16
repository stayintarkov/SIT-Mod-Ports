using SkillsExtended.Models;
using System.Collections.Generic;

namespace SkillsExtended.Helpers
{
    public static class Constants
    {
        public const float WEAPON_PROF_XP = 1.05f;                  //XP per hit
        public const float ERGO_MOD = 0.003f;                       //Ergonomics
        public const float ERGO_MOD_ELITE = 0.10f;                  //Ergonomics elite
        public const float RECOIL_REDUCTION = 0.003f;               //Recoil bonus
        public const float RECOIL_REDUCTION_ELITE = 0.09f;          //Recoil bonus elite

        public const float MEDKIT_HP_BONUS = 0.01f;                 //Medkit bonus
        public const float MEDKIT_HP_BONUS_ELITE = 0.20f;           //Medkit bonus elite
        public const float MEDICAL_SPEED_BONUS = 0.007f;
        public const float MEDICAL_SPEED_BONUS_ELITE = 0.15f;

        //public const float RESOURCE_RED_BONUS = 0.003f;           //Bleed-Fracture Resource cost reduction
        //public const float RESOURCE_RED_BONUS_ELITE = 0.20f;      //Bleed-Fracture Resource cost reduction elite

        public const float USEC_INERTIA_RED_BONUS = 0.002f;         //Usec tactics inertia reduction
        public const float USEC_INERTIA_RED_BONUS_ELITE = 0.10f;    //Usec tactics inertia reduction elite
        public const float USEC_AIMPUNCH_RED_BONUS = 0.002f;        //Usec tactics aimpunch reduction
        public const float USEC_AIMPUNCH_RED_BONUS_ELITE = 0.05f;   //Usec tactics aimpunch reduction elite 

        public const float BEAR_POWER_HP_BONUS = 0.001f;            //Bear power health bonus
        public const float BEAR_POWER_HP_BONUS_ELITE = 0.05f;       //Bear power health bonus elite
        public const float BEAR_POWER_CARRY_BONUS = 0.002f;         //Bear power carry weight bonus
        public const float BEAR_POWER_CARRY_BONUS_ELITE = 0.06f;    //Bear power carry weight bonus elite
        public const double BEAR_POWER_UPDATE_TIME = 1d;            //Bear power XP cooldown time in seconds

        // Contains key information
        public static KeysResponse Keys = new KeysResponse();

        public static readonly List<string> FIRST_AID_ITEM_LIST = new List<string>
        {
            "544fb45d4bdc2dee738b4568", // Salewa
            "5755356824597772cb798962", // AI-2
            "590c657e86f77412b013051d", // Grizzly
            "590c661e86f7741e566b646a", // Car
            "590c678286f77426c9660122", // Ifak
            "5e99711486f7744bfc4af328", // Sanitars
            "60098ad7c2240c0fe85c570a"  // AFAK
        };

        public static readonly List<string> FIELD_MEDICINE_ITEM_LIST = new List<string>
        {
            "544fb25a4bdc2dfb738b4567", // bandage
            "5751a25924597722c463c472", // army bandage
            "5e831507ea0a7c419c2f9bd9", // esmarch
            "60098af40accd37ef2175f27", // CAT
            "5e8488fa988a8701445df1e4", // calok-b
            "544fb3364bdc2d34748b456a", // splint
            "5af0454c86f7746bf20992e8"  // alu splint
        };

        public static readonly List<string> USEC_WEAPON_LIST = new List<string>
        {
            // ASSAULT RIFLES
            "62e7c4fba689e8c9c50dfc38", //AUG A1
            "63171672192e68c5460cebc5", //AUG A3
            "5c488a752e221602b412af63", //MDR 5.56
            "5dcbd56fdbd3d91b3e5468d5", //MDR 7.62
            "5bb2475ed4351e00853264e3", //HK 416A5 5.56
            "623063e994fc3f7b302a9696", //HK G36
            "5447a9cd4bdc2dbd208b4567", //M4A1
            "5fbcc1d9016cce60e8341ab3", //MCX
            "628a60ae6b1d481ff772e9c8", //RD-704
            "606587252535c57a13424cfd", //CMMG Mk47 Mutant
            "5b0bbe4e5acfc40dc528a72d", //SA-58
            "6183afd850224f204c1da514", //SCAR-H
            "6165ac306ef05c2ce828ef74", //SCAR-H FDE
            "6184055050224f204c1da540", //SCAR-L
            "618428466ef05c2ce828f218", //SCAR-L FDE
            // CARBINES
            "5f2a9575926fd9352339381f", //RFB
            "5d43021ca4b9362eab4b5e25"  //TX15
        };

        public static readonly List<string> BEAR_WEAPON_LIST = new List<string>
        {
            // ASSAULT RIFLES
            "6499849fc93611967b034949", //AK-12
            "5ac66cb05acfc40198510a10", //AK-101
            "5ac66d015acfc400180ae6e4", //AK-102
            "5ac66d2e5acfc43b321d4b53", //AK-103
            "5ac66d725acfc43b321d4b60", //AK-104
            "5ac66d9b5acfc4001633997a", //AK-105
            "5bf3e03b0db834001d2c4a9c", //AK-74
            "5ac4cd105acfc40016339859", //AK-74M
            "5644bd2b4bdc2d3b4c8b4572", //AK-74N
            "59d6088586f774275f37482f", //AKM
            "5a0ec13bfcdbcb00165aa685", //AKMN
            "59ff346386f77477562ff5e2", //AKMS
            "5abcbc27d8ce8700182eceeb", //AKMSN
            "5bf3e0490db83400196199af", //AKS-74
            "5ab8e9fcd8ce870019439434", //AKS-74N
            "57dc2fa62459775949412633", //AKS-74U
            "583990e32459771419544dd2", //AKS-74UN
            // CARBINES
            "6410733d5dd49d77bd07847e", //AVT-40
            "574d967124597745970e7c94", //SKS
            "587e02ff24597743df3deaeb", //OP-SKS
            "628b5638ad252a16da6dd245", //SAG AK
            "628b9c37a733087d0d7fe84b", //SAG AK SHORT
            "643ea5b23db6f9f57107d9fd", //SVT-40
            "5c501a4d2e221602b412b540", //VPO-101
            "59e6152586f77473dc057aa1", //VPO-136
            "59e6687d86f77411d949b251", //VPO-209
            "5c07c60e0db834002330051f"  //ADAR
        };
    }
}
