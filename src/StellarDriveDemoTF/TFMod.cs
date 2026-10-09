using System;
using System.Linq;
using System.Runtime.CompilerServices;
using MelonLoader;
using StellarDriveDemoTF;
using StellarDriveDemoTF.Common;
using StellarDriveDemoTF.Paint;

[assembly: MelonInfo(typeof(TFMod), "StellarDrive Demo TF", "0.9.0", "Romalaure")]
[assembly: MelonGame("CuriousOwlGames", "StellarDrive")]
[assembly: MelonOptionalDependencies("SDModKit")]

namespace StellarDriveDemoTF
{
    public class TFMod : MelonMod
    {
        internal static MelonLogger.Instance Log { get; private set; }

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            Settings.Load();
            PaintNet.InstallSerializers();
            TFNet.InstallSerializers();
            Devices.TeleportCapsule.Install();
            Devices.Radio.Install();
            Devices.Outfits.Install();
            Log.Msg("ready");
        }

        public override void OnLateInitializeMelon()
        {
            // Lamps, the camera, chairs, infinite parts and devices are new parts built with SDModKit; paint works without it
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "SDModKit"))
                RegisterParts();
            else
                Log.Warning("SDModKit is not installed: lamps, camera, chairs, infinite parts, radio, wardrobe and teleport capsule are disabled");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RegisterParts()
        {
            Try("the TF build tab", Common.TFTab.Declare);
            Try("lamps", Lights.LampParts.Register);
            Try("the camera", Cameras.CameraParts.Register);
            Try("chairs", Chairs.ChairParts.Register);
            Try("infinite parts", Infinite.InfiniteParts.Register);
            Try("the teleport capsule", Devices.TeleportCapsule.Register);
            Try("the radio", Devices.Radio.Register);
            Try("the wardrobe", Devices.Wardrobe.Register);
        }

        private static void Try(string what, Action register)
        {
            try
            {
                register();
            }
            catch (Exception e)
            {
                Log.Error("could not register " + what + ": " + e);
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            GameServices.Invalidate();
            ModMenu.Reset();
        }

        public override void OnUpdate()
        {
            PaintNet.Update();
            PaintToolGiver.Update();
            PaintHud.Update();
            GameDump.Update();
            Cameras.CameraTablet.Update();
            TFNet.Update();
            UsableParts.Update();
            Devices.Outfits.Update();
            Devices.RadioSound.Update();
            Devices.ThirdPerson.Update();
        }

        public override void OnLateUpdate()
        {
            Cameras.CameraTablet.LateUpdate();
        }

        public override void OnGUI()
        {
            PaintHud.Draw();
            Cameras.CameraTablet.Draw();
            UsableParts.Draw();
            Devices.TeleportMenu.Draw();
            Devices.RadioMenu.Draw();
            Devices.WardrobeMenu.Draw();
        }
    }
}
