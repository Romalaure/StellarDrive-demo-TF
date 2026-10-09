using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader.Utils;
using Newtonsoft.Json;
using StellarDriveDemoTF.Common;
using UnityEngine;

namespace StellarDriveDemoTF.Paint
{
    internal sealed class PaintPreset
    {
        public string name;
        public string color;
        public bool finish;
        public float gloss;
        public float metal;
        public float glow;

        [JsonIgnore]
        public Color Color => ColorUtility.TryParseHtmlString(color, out Color c) ? c : Color.white;

        [JsonIgnore]
        public PaintFinish Finish => finish ? PaintFinish.Of(gloss, metal, glow) : PaintFinish.None;

        public static PaintPreset From(string name, Color color, PaintFinish finish) => new PaintPreset
        {
            name = name,
            color = "#" + ColorUtility.ToHtmlStringRGB(color),
            finish = finish.Enabled,
            gloss = finish.GlossValue,
            metal = finish.MetalValue,
            glow = finish.GlowValue
        };
    }

    /// <summary>
    /// What the local player paints with, beyond the game's color selection: the finish, and the
    /// saved presets. Kept per player in UserData/TF/paint-presets.json.
    /// </summary>
    internal static class PaintBrush
    {
        private const string FileName = "paint-presets.json";

        private sealed class BrushFile
        {
            public int version = 1;
            public PaintPreset current;
            public List<PaintPreset> presets = new List<PaintPreset>();
        }

        private static bool _loaded;

        public static PaintFinish Finish = PaintFinish.None;
        public static readonly List<PaintPreset> Presets = new List<PaintPreset>();

        /// <summary>The paint a click applies right now.</summary>
        public static PaintData Current
        {
            get
            {
                var selection = GameServices.PaintSelection;
                Color color = selection != null ? selection.SelectedPaintColor : Color.white;
                return new PaintData(PaintNet.ToColor32(color), Finish);
            }
        }

        public static void EnsureLoaded()
        {
            if (_loaded)
                return;
            _loaded = true;
            try
            {
                string path = FilePath();
                if (File.Exists(path))
                {
                    var file = JsonConvert.DeserializeObject<BrushFile>(File.ReadAllText(path));
                    if (file?.presets != null)
                        Presets.AddRange(file.presets);
                    if (file?.current != null)
                        Finish = file.current.Finish;
                    return;
                }
            }
            catch (Exception e)
            {
                TFMod.Log.Error("could not read paint presets: " + e.Message);
            }
            Presets.AddRange(DefaultPresets());
        }

        public static void Save()
        {
            try
            {
                var selection = GameServices.PaintSelection;
                var file = new BrushFile
                {
                    current = PaintPreset.From("current", selection != null ? selection.SelectedPaintColor : Color.white, Finish),
                    presets = Presets
                };
                string path = FilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonConvert.SerializeObject(file, Formatting.Indented));
            }
            catch (Exception e)
            {
                TFMod.Log.Error("could not save paint presets: " + e.Message);
            }
        }

        public static void Apply(PaintPreset preset)
        {
            var selection = GameServices.PaintSelection;
            if (selection != null)
                selection.SelectedPaintColor = preset.Color;
            Finish = preset.Finish;
            Save();
        }

        public static void AddPreset(string name)
        {
            PaintData current = Current;
            Presets.Add(PaintPreset.From(string.IsNullOrWhiteSpace(name) ? "Preset " + (Presets.Count + 1) : name.Trim(), current.Color, Finish));
            Save();
        }

        public static void RemovePreset(PaintPreset preset)
        {
            Presets.Remove(preset);
            Save();
        }

        private static string FilePath() => Path.Combine(MelonEnvironment.UserDataDirectory, "TF", FileName);

        private static IEnumerable<PaintPreset> DefaultPresets()
        {
            yield return PaintPreset.From("Blanc coque", new Color(0.85f, 0.85f, 0.85f), PaintFinish.None);
            yield return PaintPreset.From("Noir mat", new Color(0.08f, 0.08f, 0.09f), PaintFinish.Of(0.05f, 0f, 0f));
            yield return PaintPreset.From("Rouge vernis", new Color(0.75f, 0.06f, 0.06f), PaintFinish.Of(0.9f, 0f, 0f));
            yield return PaintPreset.From("Bleu marine", new Color(0.07f, 0.16f, 0.38f), PaintFinish.Of(0.55f, 0.1f, 0f));
            yield return PaintPreset.From("Chrome", new Color(0.9f, 0.9f, 0.92f), PaintFinish.Of(0.95f, 1f, 0f));
            yield return PaintPreset.From("Or", new Color(1f, 0.76f, 0.3f), PaintFinish.Of(0.85f, 1f, 0f));
            yield return PaintPreset.From("Cuivre", new Color(0.85f, 0.45f, 0.25f), PaintFinish.Of(0.7f, 1f, 0f));
            yield return PaintPreset.From("Néon cyan", new Color(0.1f, 0.95f, 1f), PaintFinish.Of(0.6f, 0f, 0.6f));
            yield return PaintPreset.From("Néon orange", new Color(1f, 0.45f, 0.05f), PaintFinish.Of(0.6f, 0f, 0.6f));
            yield return PaintPreset.From("Vert militaire", new Color(0.26f, 0.32f, 0.18f), PaintFinish.Of(0.15f, 0f, 0f));
        }
    }
}
