using SDModKit.Game;
using StellarDriveDemoTF.Lights;

namespace StellarDriveDemoTF.Common
{
    /// <summary>
    /// The build menu tabs holding the TF parts, one row per family. The menu has no scrolling,
    /// so the parts are split over two tabs of three rows: decoration, and objects.
    /// </summary>
    internal static class TFTab
    {
        public const string Name = "TF";
        public const string ObjectsName = "TF Objets";

        // "TF": lamps (two rows of 8) and chairs
        public const int LampsRow = 0;
        public const int ChairsRow = 2;

        // "TF Objets": camera and devices, infinite parts, floor doors
        public const int DevicesRow = 0;
        public const int CamerasRow = DevicesRow;
        public const int InfiniteRow = 1;
        public const int DoorsRow = 2;

        /// <summary>Only call when SDModKit is loaded.</summary>
        public static void Declare()
        {
            BuildTabs.Declare(Name, 50, LampIcon.Create());
            BuildTabs.Declare(ObjectsName, 51, LampIcon.Create());
        }
    }
}
