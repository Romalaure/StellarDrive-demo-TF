using SDModKit.Game;
using StellarDriveDemoTF.Lights;

namespace StellarDriveDemoTF.Common
{
    /// <summary>The one build menu tab holding every TF part, one row per family.</summary>
    internal static class TFTab
    {
        public const string Name = "TF";

        public const int LampsRow = 0;
        public const int CamerasRow = 1;
        public const int ChairsRow = 2;
        public const int InfiniteRow = 3;

        /// <summary>Only call when SDModKit is loaded.</summary>
        public static void Declare() => BuildTabs.Declare(Name, 50, LampIcon.Create());
    }
}
