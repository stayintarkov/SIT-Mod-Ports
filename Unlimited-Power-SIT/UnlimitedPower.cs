using EFT;
using System;
using UnityEngine;
using Comfort.Common;
using EFT.Interactive;
using System.Reflection;
using System.Collections;
using EFT.Communications;

namespace Power
{
    public class UnlimitedPower : MonoBehaviour
    {
        private bool _isRunning = false;
        private Switch[] _switchs = null;

        Player player
        { get => gameWorld.MainPlayer; }

        GameWorld gameWorld
        { get => Singleton<GameWorld>.Instance; }

        void Update()
        {
            // We dont want to run if the game world is null or
            // The plugin isnt enabled or
            // The component is actively running or
            if (!Ready() || !Plugin.Enablemod.Value || _isRunning)
            {
                return;
            }

            // Cache the switches just once
            if (_switchs == null)
            {
                _switchs = FindObjectsOfType<Switch>();
            }

            // Check that switches exist before starting the coroutine
            // Start the script on a seperate thread if there are switches to activate
            // Set the _isRunning flag to true so we dont start multiple instances
            if ( _switchs.Length > 0 )
            {
                StaticManager.Instance.StartCoroutine(ThrowRandomSwitch());
                _isRunning = true;
            }
        }

        // This is an extremely fancy loop that is very effiecient in terms of cpu time
        // it will run the timer until its ready to move on to the next instruction
        private IEnumerator ThrowRandomSwitch()
        {
            // Do nothing until its time to turn on a switch
            // Multiplied by 60f because we convert from minutes to seconds
            yield return new WaitForSeconds(UnityEngine.Random.Range(Plugin.RandomRangeMin.Value, Plugin.RandomRangeMax.Value) * 60f);
            
            // Once the timer is complete turn on a random switch
            PowerOn();

            // Set _isRunning bool to false
            // Then break out of the coroutine because we are finished
            _isRunning = false;
            yield break;
        }

        private void PowerOn()
        {
            System.Random random = new System.Random();
            
            // Pick a random switch from the array
            int selection = random.Next(_switchs.Length);
            Switch _switch = _switchs[selection];

            // Invoke the Open method on the selected switch
            typeof(Switch).GetMethod("Open", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_switch, null);
            
            // Send the player a notification of the event
            if (Plugin.ShowNotification.Value) NotificationManagerClass.DisplayMessageNotification("Unlimited Power: A random switch has been thrown.", ENotificationDurationType.Default);

            // Remove the switch from the list 
            // to avoid duplicate attempts on the same switch.
            RemoveAt(ref _switchs, selection);
        }
        
        // Helper method to remove an element at a specific index from an array
        static void RemoveAt<T>(ref T[] array, int index)
        {
            if (index >= 0 && index < array.Length)
            {
                for (int i = index; i < array.Length - 1; i++)
                {
                    array[i] = array[i + 1];
                }

                Array.Resize(ref array, array.Length - 1);
            }
        }

        private bool Ready() => gameWorld != null && gameWorld.AllAlivePlayersList != null && gameWorld.AllAlivePlayersList.Count > 0 && !(player is HideoutPlayer);
    }
}
