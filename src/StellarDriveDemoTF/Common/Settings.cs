using MelonLoader;

namespace StellarDriveDemoTF.Common
{
    internal static class Settings
    {
        private static MelonPreferences_Category _category;

        public static MelonPreferences_Entry<string> GivePaintToolKey { get; private set; }
        public static MelonPreferences_Entry<bool> PaintAllParts { get; private set; }
        public static MelonPreferences_Entry<bool> DumpOnWorldLoad { get; private set; }
        public static MelonPreferences_Entry<bool> AdaptiveLamps { get; private set; }
        public static MelonPreferences_Entry<bool> CopilotCanFly { get; private set; }
        public static MelonPreferences_Entry<string> TabletKey { get; private set; }
        public static MelonPreferences_Entry<int> CameraFps { get; private set; }
        public static MelonPreferences_Entry<int> CameraResolution { get; private set; }

        public static void Load()
        {
            _category = MelonPreferences.CreateCategory("TF", "StellarDrive Demo TF");
            GivePaintToolKey = _category.CreateEntry("GivePaintToolKey", "F8",
                description: "Key (UnityEngine.InputSystem.Key name) that puts the paint tool in your inventory if you do not have one. Empty disables it.");
            PaintAllParts = _category.CreateEntry("PaintAllParts", true,
                description: "Let the paint tool color doors, machines and every other placed part, not only walls and floors.");
            DumpOnWorldLoad = _category.CreateEntry("DumpOnWorldLoad", true,
                description: "Write every item and part definition to UserData/TF/dump.txt the first time a world loads. For mod development.");
            AdaptiveLamps = _category.CreateEntry("AdaptiveLamps", true,
                description: "Lamps shine brighter at night and indoors, softer in open daylight.");
            CopilotCanFly = _category.CreateEntry("CopilotCanFly", false,
                description: "Let the TF copilot seat fly the ship like the pilot seat. Off: like every TF chair, it has no effect on the ship.");
            TabletKey = _category.CreateEntry("TabletKey", "F9",
                description: "Key (UnityEngine.InputSystem.Key name) that opens and closes the camera tablet. Empty disables it.");
            CameraFps = _category.CreateEntry("CameraFps", 20,
                description: "Refreshes per second of the camera image on the tablet. Cameras only render while the tablet is open.");
            CameraResolution = _category.CreateEntry("CameraResolution", 640,
                description: "Width in pixels of the camera image on the tablet.");
        }
    }
}
