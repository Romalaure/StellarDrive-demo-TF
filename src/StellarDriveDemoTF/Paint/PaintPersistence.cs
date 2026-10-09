using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Managers.Server.Saving;
using Newtonsoft.Json;
using Saving.Services;
using Ships.Interface.Model.Parts;
using StellarDriveDemoTF.Common;
using UnityEngine;

namespace StellarDriveDemoTF.Paint
{
    /// <summary>
    /// Part colors are kept in tf-paint.json next to the world's world.json, so the game's own
    /// save stays untouched and loads fine without the mod.
    /// </summary>
    internal static class PaintPersistence
    {
        private const string FileName = "tf-paint.json";
        private const int FormatVersion = 1;

        private sealed class SaveFile
        {
            public int version = FormatVersion;
            public List<SavedColor> parts = new List<SavedColor>();
        }

        private sealed class SavedColor
        {
            public uint ship;
            public ushort part;
            public string color;
        }

        public static void Save(string worldDirectory)
        {
            PruneRemovedParts();

            var file = new SaveFile
            {
                parts = PaintNet.Server.All
                    .OrderBy(e => e.Key.ShipId).ThenBy(e => e.Key.PartId)
                    .Select(e => new SavedColor { ship = e.Key.ShipId, part = e.Key.PartId, color = "#" + ColorUtility.ToHtmlStringRGB(e.Value) })
                    .ToList()
            };

            string path = Path.Combine(worldDirectory, FileName);
            if (file.parts.Count == 0)
            {
                if (File.Exists(path))
                    File.Delete(path);
                return;
            }

            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonConvert.SerializeObject(file, Formatting.Indented));
            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }

        public static void Load(string worldDirectory)
        {
            PaintNet.Server.Clear();
            string path = Path.Combine(worldDirectory, FileName);
            if (!File.Exists(path))
                return;

            var file = JsonConvert.DeserializeObject<SaveFile>(File.ReadAllText(path));
            if (file?.parts == null)
                return;

            foreach (SavedColor saved in file.parts)
            {
                if (saved?.color != null && ColorUtility.TryParseHtmlString(saved.color, out Color color))
                    PaintNet.Server.Set(new PartKey(saved.ship, saved.part), color);
            }
            TFMod.Log.Msg($"loaded {PaintNet.Server.Count} painted part(s)");
        }

        // Drops colors of parts that no longer exist (ship destroyed, part gone while unloaded, ...)
        private static void PruneRemovedParts()
        {
            var ships = GameServices.ShipsServer;
            if (ships == null)
                return;
            foreach (PartKey key in PaintNet.Server.All.Select(e => e.Key).ToList())
            {
                if (!ships.TryGetStatefulPart(new PartContext(key.ShipId, key.PartId), out _))
                    PaintNet.Server.Remove(key);
            }
        }
    }

    [HarmonyPatch(typeof(GameSaver), "SaveGame", typeof(string))]
    internal static class GameSaverPatch
    {
        private static void Postfix(string worldDirectoryFullName)
        {
            try
            {
                PaintPersistence.Save(worldDirectoryFullName);
            }
            catch (Exception e)
            {
                TFMod.Log.Error("could not save part colors: " + e);
            }
        }
    }

    [HarmonyPatch(typeof(ServerStateLoader), "LoadWorldFromPathAndName")]
    internal static class ServerStateLoaderPatch
    {
        private static void Prefix(string path)
        {
            try
            {
                PaintPersistence.Load(path);
            }
            catch (Exception e)
            {
                TFMod.Log.Error("could not load part colors: " + e);
            }
        }
    }
}
