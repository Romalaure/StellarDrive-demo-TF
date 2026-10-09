using MelonLoader;
using StellarDriveDemoTF;
using StellarDriveDemoTF.Common;
using StellarDriveDemoTF.Paint;

[assembly: MelonInfo(typeof(TFMod), "StellarDrive Demo TF", "0.4.0", "Romalaure")]
[assembly: MelonGame("CuriousOwlGames", "StellarDrive")]

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
