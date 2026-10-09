using System;
using System.IO;
using System.Linq;
using System.Text;
using Items.Model;
using MelonLoader.Utils;
using Ships.Interface.Settings;
using UnityEngine;

namespace StellarDriveDemoTF.Common
{
    /// <summary>
    /// Development aid: writes every item and part definition, with the renderers, materials
    /// and lights of each part prefab, to UserData/TF/dump.txt once per game session.
    /// </summary>
    internal static class GameDump
    {
        private static bool _done;

        public static void Update()
        {
            if (_done || !Settings.DumpOnWorldLoad.Value || GameServices.ShipsClient == null)
                return;
            _done = true;
            try
            {
                string directory = Path.Combine(MelonEnvironment.UserDataDirectory, "TF");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "dump.txt");
                File.WriteAllText(path, Build());
                TFMod.Log.Msg("wrote " + path);
            }
            catch (Exception e)
            {
                TFMod.Log.Error("dump failed: " + e);
            }
        }

        private static string Build()
        {
            var text = new StringBuilder();

            text.AppendLine("== ITEMS");
            foreach (ItemSettings item in Resources.FindObjectsOfTypeAll<ItemSettings>().Where(i => i != null).OrderBy(i => i.id))
            {
                string tool = item.toolSettings != null && item.toolSettings.prefab != null
                    ? " tool=" + item.toolSettings.prefab.name + " [" + string.Join(",", item.toolSettings.prefab.GetComponents<MonoBehaviour>().Where(c => c != null).Select(c => c.GetType().Name)) + "]"
                    : "";
                text.AppendLine($"{item.id}\t{item.name}\t\"{item.itemName}\"\tstack={item.maxStackSize}{tool}");
            }

            text.AppendLine();
            text.AppendLine("== PARTS");
            foreach (PartSettings part in Resources.FindObjectsOfTypeAll<PartSettings>().Where(p => p != null).OrderBy(p => p.id))
            {
                text.AppendLine($"{part.id}\t{part.name}\t\"{part.fullLabel}\"\tstate={part.internalStateClass}\ttype={part.GetType().Name}\tsize={part.size}\tmass={part.mass}\tsnap={part.snappingStyle}");
                if (part.part == null)
                    continue;
                foreach (Renderer renderer in part.part.GetComponentsInChildren<Renderer>(true))
                {
                    string materials = string.Join(", ", renderer.sharedMaterials.Select(m => m == null ? "null" : $"{m.name}({(m.shader != null ? m.shader.name : "?")},q{m.renderQueue})"));
                    text.AppendLine($"    renderer {HierarchyPath(renderer.transform, part.part.transform)}: {materials}");
                }
                foreach (Light light in part.part.GetComponentsInChildren<Light>(true))
                    text.AppendLine($"    light {HierarchyPath(light.transform, part.part.transform)}: {light.type} range={light.range} intensity={light.intensity} color={light.color}");
                text.AppendLine("    components: " + string.Join(", ", part.part.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null).Select(c => c.GetType().Name).Distinct()));
            }
            return text.ToString();
        }

        private static string HierarchyPath(Transform transform, Transform root)
        {
            string path = transform.name;
            while (transform != root && transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
}
