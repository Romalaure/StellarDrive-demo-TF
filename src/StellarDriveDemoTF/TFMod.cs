using System;
using System.Linq;
using System.Runtime.CompilerServices;
using MelonLoader;
using StellarDriveDemoTF;
using StellarDriveDemoTF.Common;
using StellarDriveDemoTF.Paint;

[assembly: MelonInfo(typeof(TFMod), "StellarDrive Demo TF", "0.6.0", "Romalaure")]
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
            Log.Msg("ready");
        }

        public override void OnLateInitializeMelon()
        {
            // Lamps, mirrors and infinite parts are new parts built with SDModKit; paint works without it
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "SDModKit"))
                RegisterParts();
            else
                Log.Warning("SDModKit is not installed: lamps, mirrors and infinite parts are disabled");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RegisterParts()
        {
            try
            {
                Lights.LampParts.Register();
            }
            catch (Exception e)
            {
                Log.Error("could not register lamps: " + e);
            }
            try
            {
                Infinite.InfiniteParts.Register();
            }
            catch (Exception e)
            {
                Log.Error("could not register infinite parts: " + e);
            }
            try
            {
                Chairs.ChairParts.Register();
            }
            catch (Exception e)
            {
                Log.Error("could not register chairs: " + e);
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            GameServices.Invalidate();
        }

        public override void OnUpdate()
        {
            PaintNet.Update();
            PaintToolGiver.Update();
            PaintHud.Update();
            GameDump.Update();
        }

        public override void OnGUI()
        {
            PaintHud.Draw();
        }
    }
}
