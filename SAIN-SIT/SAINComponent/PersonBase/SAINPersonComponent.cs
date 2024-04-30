using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Info;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.BaseClasses
{
    public class SAINPersonComponent : MonoBehaviour
    {
        public static bool TryAddSAINPersonToBot(BotOwner botOwner, out SAINPersonComponent component)
        {
            Player player = EFTInfo.GetPlayer(botOwner?.ProfileId);
            GameObject gameObject = player?.gameObject;

            if (TryAddSAINPersonToPlayer(player, out component))
            {
                return true;
            }
            return false;
        }

        public static bool TryAddSAINPersonToPlayer(Player player, out SAINPersonComponent component)
        {
            GameObject gameObject = player?.gameObject;
            if (gameObject != null && player != null)
            {
                // If Somehow this player already has SAINPersonComponent attached, destroy it.
                if (gameObject.TryGetComponent(out component))
                {
                    GameObject.Destroy(component);
                }

                // Create a new Component
                component = gameObject.AddComponent<SAINPersonComponent>();

                // Check is component is successfully initialized
                if (component?.Init(player) == true)
                {
                    return true;
                };
            }
            component = null;
            return false;
        }

        public bool Init(IPlayer person)
        {
            if (person == null)
            {
                Logger.LogError("person == null");
                return false;
            }
            else
            {
                SAINPerson = new SAINPersonClass(person);
                return true;
            }
        }

        private void Awake()
        {
        }

        public void Update()
        {
            SAINPerson?.Update();
        }

        public IPlayer IPlayer => SAINPerson.IPlayer;
        public Player Player => SAINPerson.Player;
        public SAINPersonClass SAINPerson { get; private set; }

    }
}