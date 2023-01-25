using HarmonyLib;
using MelonLoader;
using System;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading;

namespace Provolver_SuperHot
{
    public class SuperhotVR_Provolver : MelonMod
    {
        public static string configPath = Directory.GetCurrentDirectory() + "\\Mods\\dualwield\\";
        public static bool dualWield = false;
        private MelonPreferences_Category config;
        public static bool leftHanded = false;

        public override void OnApplicationStart()
        {
            config = MelonPreferences.CreateCategory("provolver");
            config.CreateEntry<bool>("leftHanded", false);
            config.SetFilePath("Mods/Provolver/Provolver_config.cfg");
            leftHanded = bool.Parse(config.GetEntry("leftHanded").GetValueAsString());
            InitializeProTube();
        }

        public static void saveChannel(string channelName, string proTubeName)
        {
            string fileName = configPath + channelName + ".pro";
            File.WriteAllText(fileName, proTubeName, Encoding.UTF8);
        }

        public static string readChannel(string channelName)
        {
            string fileName = configPath + channelName + ".pro";
            if (!File.Exists(fileName)) return "";
            return File.ReadAllText(fileName, Encoding.UTF8);
        }

        public static void dualWieldSort()
        {
            ForceTubeVRInterface.FTChannelFile myChannels = JsonConvert.DeserializeObject<ForceTubeVRInterface.FTChannelFile>(ForceTubeVRInterface.ListChannels());
            var pistol1 = myChannels.channels.pistol1;
            var pistol2 = myChannels.channels.pistol2;
            if ((pistol1.Count > 0) && (pistol2.Count > 0))
            {
                dualWield = true;
                MelonLogger.Msg("Two ProTube devices detected, player is dual wielding.");
                if ((readChannel("rightHand") == "") || (readChannel("leftHand") == ""))
                {
                    MelonLogger.Msg("No configuration files found, saving current right and left hand pistols.");
                    saveChannel("rightHand", pistol1[0].name);
                    saveChannel("leftHand", pistol2[0].name);
                }
                else
                {
                    string rightHand = readChannel("rightHand");
                    string leftHand = readChannel("leftHand");
                    MelonLogger.Msg("Found and loaded configuration. Right hand: " + rightHand + ", Left hand: " + leftHand);
                    // Channels 4 and 5 are ForceTubeVRChannel.pistol1 and pistol2
                    ForceTubeVRInterface.ClearChannel(4);
                    ForceTubeVRInterface.ClearChannel(5);
                    ForceTubeVRInterface.AddToChannel(4, rightHand);
                    ForceTubeVRInterface.AddToChannel(5, leftHand);
                }
            }
            else
            {
                MelonLogger.Msg("SINGLE WIELD");
            }
        }
        private async void InitializeProTube()
        {
            MelonLogger.Msg("Initializing ProTube gear...");
            await ForceTubeVRInterface.InitAsync(true);
            Thread.Sleep(10000);
            dualWieldSort();
        }
    }


    [HarmonyPatch(typeof(ShootingSystem), "UpdateShootingFor")]
    public class provolver_ShootingSystem_UpdateShootingFor
    {
        [HarmonyPrefix]
        public static void Prefix(ShootingSystem __instance, VrHandController handController)
        {
            if (!__instance.PickupCanShot(handController.CurrentPickup) || !handController.InteractionReady)
                return;
            GunPickup pickup = handController.CurrentPickup.GetPickup() as GunPickup;

            if (pickup.Gun.ammoCount == 0 || pickup.Gun is UziGun)
            {
                return;
            }

            if (VrInputSystem.GetTriggerDown(handController.Controller))
            {
                ForceTubeVRChannel myChannel = (handController.Controller == Controller.RightController)
                    ?
                    (SuperhotVR_Provolver.leftHanded && !SuperhotVR_Provolver.dualWield) ?
                        ForceTubeVRChannel.pistol2 : ForceTubeVRChannel.pistol1
                    :
                    (SuperhotVR_Provolver.leftHanded && !SuperhotVR_Provolver.dualWield) ?
                        ForceTubeVRChannel.pistol1 : ForceTubeVRChannel.pistol2;

                if (pickup.Gun is ShotGun)
                    ForceTubeVRInterface.Shoot(255, 210, 50f, myChannel);
                if (pickup.Gun is PistolGun)
                    ForceTubeVRInterface.Kick(255, myChannel);
            }
        }
    }

    [HarmonyPatch(typeof(UziGun), "ShootUziBullets")]
    public class provolver_UziGun_ShootUziBullets
    {
        [HarmonyPrefix]
        public static void Prefix(UziGun __instance)
        {
            VrHandController hand = __instance.gameObject.GetComponentInParent<VrHandController>();
            if (hand)
            {
                if (hand.Controller == Controller.LeftController)
                {
                    ForceTubeVRInterface.Kick(210, 
                        (SuperhotVR_Provolver.leftHanded && !SuperhotVR_Provolver.dualWield) 
                        ? ForceTubeVRChannel.pistol1 : ForceTubeVRChannel.pistol2);
                }
                if (hand.Controller == Controller.RightController)
                {
                    ForceTubeVRInterface.Kick(210, 
                        (SuperhotVR_Provolver.leftHanded && !SuperhotVR_Provolver.dualWield)
                        ? ForceTubeVRChannel.pistol2 : ForceTubeVRChannel.pistol1);
                }
            }
        }
    }
}
