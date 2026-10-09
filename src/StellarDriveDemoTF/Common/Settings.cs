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
            MirrorResolution = _category.CreateEntry("MirrorResolution", 512,
                description: "Largest width in pixels of a mirror's image. Mirrors that look small on screen use less.");
            MirrorMaxDistance = _category.CreateEntry("MirrorMaxDistance", 20f,
                description: "Mirrors farther than this (meters) from you stop updating.");
            MirrorFps = _category.CreateEntry("MirrorFps", 30,
                description: "How many times per second a mirror refreshes its image.");
            MirrorMaxActive = _category.CreateEntry("MirrorMaxActive", 2,
                description: "How many mirrors (the nearest ones in view) may refresh at the same time.");
            MirrorFarClip = _category.CreateEntry("MirrorFarClip", 80f,
                description: "Mirrors only reflect what is closer than this (meters). Far scenery is the costliest part.");
        }
    }
}
