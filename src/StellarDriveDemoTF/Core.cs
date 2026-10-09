using MelonLoader;
using StellarDriveDemoTF;

[assembly: MelonInfo(typeof(Core), "StellarDrive Demo TF", "0.1.0", "Romalaure")]
[assembly: MelonGame("CuriousOwlGames", "StellarDrive")]

namespace StellarDriveDemoTF
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("ready");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg($"scene loaded: {sceneName} ({buildIndex})");
        }
    }
}
