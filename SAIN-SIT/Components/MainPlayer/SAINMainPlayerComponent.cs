using Comfort.Common;
using EFT;
using SAIN.SAINComponent.BaseClasses;
using SAIN.SAINComponent.Classes.Mover;
using UnityEngine;

namespace SAIN.Components
{
    public class SAINMainPlayerComponent : MonoBehaviour
    {
        public void Awake()
        {
            CamoClass = new SAINCamoClass(this);
        }

        public SAINCamoClass CamoClass { get; private set; }

        private void Start()
        {
            CamoClass.Start();
        }

        private void Update()
        {
            //CamoClass.Update();
            SAINPerson?.Update();

            if (MainPlayer != null && SAINPerson == null)
            {
                SAINPerson = new SAINPersonClass(MainPlayer);
                //SAINVaultClass.DebugCheckObstacles(MainPlayer);
            }
        }

        public SAINPersonClass SAINPerson { get; private set; }

        private void OnDestroy()
        {
            CamoClass.OnDestroy();
        }

        private void OnGUI()
        {
            //CamoClass.OnGUI();
        }

        public Player MainPlayer => Singleton<GameWorld>.Instance?.MainPlayer;
    }
}