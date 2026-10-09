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
            // Lamps are new parts built with SDModKit; everything else works without it
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "SDModKit"))
                RegisterLamps();
            else
                Log.Warning("SDModKit is not installed: lamps are disabled");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RegisterLamps()
        {
            try
            {
                Lights.LampParts.Register();
            }
            catch (Exception e)
            {
                Log.Error("could not register lamps: " + e);
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
