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
        public static MelonPreferences_Entry<int> MirrorResolution { get; private set; }
        public static MelonPreferences_Entry<float> MirrorMaxDistance { get; private set; }
        public static MelonPreferences_Entry<int> MirrorFps { get; private set; }
        public static MelonPreferences_Entry<int> MirrorMaxActive { get; private set; }
        public static MelonPreferences_Entry<float> MirrorFarClip { get; private set; }
        public static MelonPreferences_Entry<float> MirrorIdleFps { get; private set; }
        public static MelonPreferences_Entry<bool> MirrorReflectWorld { get; private set; }
        public static MelonPreferences_Entry<bool> CopilotCanFly { get; private set; }

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
            // Mirror entries were renamed in 0.7.1 so the lighter defaults replace values saved by 0.7.0
            MirrorResolution = _category.CreateEntry("MirrorMaxResolution", 320,
                description: "Largest width in pixels of a mirror's image. Mirrors that look small on screen use less.");
            MirrorMaxDistance = _category.CreateEntry("MirrorMaxDistance", 20f,
                description: "Mirrors farther than this (meters) from you stop updating.");
            MirrorFps = _category.CreateEntry("MirrorMovingFps", 20,
                description: "Refreshes per second of a mirror while you move in front of it.");
            MirrorIdleFps = _category.CreateEntry("MirrorIdleFps", 2f,
                description: "Refreshes per second of a mirror while you stand still (only things moving in it need this).");
            MirrorMaxActive = _category.CreateEntry("MirrorCount", 1,
                description: "How many mirrors (the nearest ones in view) may refresh. Others keep their last image.");
            MirrorFarClip = _category.CreateEntry("MirrorViewDistance", 40f,
                description: "Mirrors only reflect what is closer than this (meters).");
            MirrorReflectWorld = _category.CreateEntry("MirrorReflectWorld", false,
                description: "Also reflect the planet, water and distant scenery (costly). Off: only the ship, players and objects, over a sky-colored background.");
            CopilotCanFly = _category.CreateEntry("CopilotCanFly", false,
                description: "Let the TF copilot seat fly the ship like the pilot seat. Off: like every TF chair, it has no effect on the ship.");
        }
    }
}
