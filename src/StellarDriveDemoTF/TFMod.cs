using System;
using System.Linq;
using System.Runtime.CompilerServices;
using MelonLoader;
using StellarDriveDemoTF;
using StellarDriveDemoTF.Common;
using StellarDriveDemoTF.Paint;

[assembly: MelonInfo(typeof(TFMod), "StellarDrive Demo TF", "0.11.0", "Romalaure")]
[assembly: MelonGame("CuriousOwlGames", "StellarDrive")]
[assembly: MelonOptionalDependencies("SDModKit")]
[assembly: HarmonyDontPatchAll]

namespace StellarDriveDemoTF
{
    public class TFMod : MelonMod
    {
        internal static MelonLogger.Instance Log { get; private set; }

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            Settings.Load();
            ApplyPatches();
            PaintNet.InstallSerializers();
            TFNet.InstallSerializers();
            Devices.TeleportCapsule.Install();
            Devices.Radio.Install();
            Devices.Schematics.Install();
            Devices.Outfits.Install();
            Devices.ToolRack.Install();
            Log.Msg("ready");
        }

        // One patch class at a time: if a game update breaks one, the others (and the game) keep working
        private void ApplyPatches()
        {
            Type[] types;
            try
            {
                types = typeof(TFMod).Assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).ToArray();
            }
            int applied = 0, failed = 0;
            foreach (Type type in types.Where(t => t.GetCustomAttributes(typeof(HarmonyLib.HarmonyPatch), false).Length > 0))
            {
                try
                {
                    HarmonyInstance.CreateClassProcessor(type).Patch();
                    applied++;
                }
                catch (Exception e)
                {
                    failed++;
                    Log.Error($"patch {type.Name} not applied, that feature is off: {e.GetBaseException().Message}");
                }
            }
            Log.Msg($"{applied} patch(es) applied" + (failed > 0 ? $", {failed} failed" : ""));
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
            Try("the tool rack", Devices.ToolRack.Register);
            Try("trapdoors and horizontal docking doors", Devices.HullDoors.Register);
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
            PaintHud.Update();
            GameDump.Update();
            Cameras.CameraTablet.Update();
            TFNet.Update();
            UsableParts.Update();
            Devices.Outfits.Update();
            Devices.RadioSound.Update();
            Devices.ThirdPerson.Update();
            Devices.SchematicTablet.Update();
            Devices.RotorCheck.Update();
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
            Devices.SchematicsMenu.Draw();
            Devices.ToolRackMenu.Draw();
            Devices.SchematicTablet.Draw();
            Devices.RotorCheck.Draw();
        }
    }
}
