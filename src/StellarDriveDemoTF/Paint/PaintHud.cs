using Core.Services;
using Players.Interface.Services;
using StellarDriveDemoTF.Common;
using UI.Interface.Services;
using UnityEngine;

namespace StellarDriveDemoTF.Paint
{
    /// <summary>
    /// Paint HUD. While the paint tool is held, a compact card shows the current paint and target.
    /// While the game's paint menu is open (right click), a side panel adds faces, finish, quick
    /// colors and presets next to the game's own hue/saturation/value pickers.
    /// </summary>
    internal static class PaintHud
    {
        public static bool ToolActive;
        public static bool MenuOpen;

        private const float PanelWidth = 380f;
        private const float CardWidth = 330f;

        private static readonly Color Background = new Color(0.07f, 0.08f, 0.1f, 0.92f);
        private static readonly Color Section = new Color(0.12f, 0.13f, 0.16f, 0.95f);
        private static readonly Color Accent = new Color(0.96f, 0.47f, 0.08f);
        private static readonly Color TextColor = new Color(0.92f, 0.93f, 0.95f);
        private static readonly Color MutedText = new Color(0.62f, 0.65f, 0.7f);

        private static readonly Color[] QuickColors =
        {
            new Color(0.95f, 0.95f, 0.95f), new Color(0.6f, 0.62f, 0.65f), new Color(0.3f, 0.31f, 0.33f), new Color(0.06f, 0.06f, 0.07f),
            new Color(0.8f, 0.1f, 0.1f), new Color(0.96f, 0.47f, 0.08f), new Color(0.98f, 0.82f, 0.15f), new Color(0.45f, 0.75f, 0.15f),
            new Color(0.12f, 0.45f, 0.22f), new Color(0.1f, 0.7f, 0.75f), new Color(0.15f, 0.4f, 0.9f), new Color(0.08f, 0.15f, 0.38f),
            new Color(0.5f, 0.2f, 0.75f), new Color(0.9f, 0.35f, 0.65f), new Color(0.45f, 0.28f, 0.15f), new Color(0.82f, 0.7f, 0.5f)
        };

        private static readonly (string Name, PaintFinish Finish)[] QuickFinishes =
        {
            ("D'origine", PaintFinish.None),
            ("Mat", PaintFinish.Of(0.05f, 0f, 0f)),
            ("Satiné", PaintFinish.Of(0.45f, 0f, 0f)),
            ("Brillant", PaintFinish.Of(0.92f, 0f, 0f)),
            ("Métal brossé", PaintFinish.Of(0.45f, 1f, 0f)),
            ("Chrome", PaintFinish.Of(0.97f, 1f, 0f)),
            ("Néon", PaintFinish.Of(0.6f, 0f, 0.7f))
        };

        private static Texture2D _white;
        private static GUIStyle _title, _label, _muted, _big, _button, _selectedButton, _field, _box;
        private static Vector2 _presetScroll;
        private static string _hexInput = "";
        private static string _presetName = "";
        private static bool _refreshGameMenu;
        private static bool _finishDirty;

        public static void Update()
        {
            if (_refreshGameMenu)
            {
                _refreshGameMenu = false;
                // The game's menu reads the selection only when it opens
                var activator = ServiceLocator.GetService<IPaintMenuActivator>();
                if (activator != null && MenuOpen)
                {
                    activator.Close();
                    activator.Open();
                }
            }
            if (_finishDirty && !MenuOpen)
            {
                _finishDirty = false;
                PaintBrush.Save();
            }
        }

        public static void Draw()
        {
            if (!ToolActive)
                return;
            var selection = GameServices.PaintSelection;
            if (selection == null)
                return;

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 2.5f);
            EnsureStyles();
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            if (MenuOpen)
                DrawPanel(selection, width, height);
            else
                DrawCard(selection, width, height);

            GUI.matrix = previous;
        }

        private static void DrawCard(IPlayerPaintToolSelectionTracker selection, float width, float height)
        {
            var rect = new Rect(width - CardWidth - 24f, height - 236f, CardWidth, 200f);
            Fill(rect, Background);
            Fill(new Rect(rect.x, rect.y, rect.width, 3f), Accent);

            GUILayout.BeginArea(new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, rect.height - 20f));
            GUILayout.Label("PINCEAU TF", _title);
            GUILayout.BeginHorizontal();
            Rect swatch = GUILayoutUtility.GetRect(64f, 64f, GUILayout.Width(64f), GUILayout.Height(64f));
            DrawSwatch(swatch, selection.SelectedPaintColor, PaintBrush.Finish);
            GUILayout.Space(12f);
            GUILayout.BeginVertical();
            GUILayout.Label("#" + ColorUtility.ToHtmlStringRGB(selection.SelectedPaintColor), _big);
            GUILayout.Label(FinishText(PaintBrush.Finish), _label);
            GUILayout.Label(selection.PaintBothSides ? "Murs : les deux faces" : "Murs : face visée seulement", _label);
            if (selection.ReplaceAllIdenticalColors)
                GUILayout.Label("Remplace toutes les zones identiques", _label);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(TargetText(), _muted);
            GUILayout.Label("Clic G peindre · Clic D menu · Molette pipette", _muted);
            GUILayout.EndArea();
        }

        private static void DrawPanel(IPlayerPaintToolSelectionTracker selection, float width, float height)
        {
            var rect = new Rect(width - PanelWidth - 24f, 40f, PanelWidth, height - 80f);
            Fill(rect, Background);
            Fill(new Rect(rect.x, rect.y, rect.width, 3f), Accent);

            GUILayout.BeginArea(new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, rect.height - 24f));
            GUILayout.Label("PINCEAU TF", _title);

            // Current paint
            GUILayout.BeginHorizontal();
            Rect swatch = GUILayoutUtility.GetRect(72f, 72f, GUILayout.Width(72f), GUILayout.Height(72f));
            DrawSwatch(swatch, selection.SelectedPaintColor, PaintBrush.Finish);
            GUILayout.Space(12f);
            GUILayout.BeginVertical();
            GUILayout.Label("#" + ColorUtility.ToHtmlStringRGB(selection.SelectedPaintColor), _big);
            GUILayout.Label(FinishText(PaintBrush.Finish), _label);
            GUILayout.BeginHorizontal();
            _hexInput = GUILayout.TextField(_hexInput, 7, _field, GUILayout.Width(110f));
            if (GUILayout.Button("Appliquer #", _button, GUILayout.Width(110f)) && ColorUtility.TryParseHtmlString(Hex(_hexInput), out Color typed))
                SetColor(selection, typed);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            // Quick colors
            SectionHeader("Couleurs rapides");
            for (int row = 0; row < QuickColors.Length / 8; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < 8; col++)
                {
                    Color color = QuickColors[row * 8 + col];
                    Rect cell = GUILayoutUtility.GetRect(36f, 28f, GUILayout.Width(36f), GUILayout.Height(28f));
                    Fill(cell, color);
                    if (GUI.Button(cell, GUIContent.none, GUIStyle.none))
                        SetColor(selection, color);
                }
                GUILayout.EndHorizontal();
            }

            // Faces
            SectionHeader("Murs et sols");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Face visée", selection.PaintBothSides ? _button : _selectedButton))
                SetBothSides(selection, false);
            if (GUILayout.Button("Les deux faces", selection.PaintBothSides ? _selectedButton : _button))
                SetBothSides(selection, true);
            GUILayout.EndHorizontal();
            bool replace = GUILayout.Toggle(selection.ReplaceAllIdenticalColors, " Remplacer toutes les zones de la même couleur");
            if (replace != selection.ReplaceAllIdenticalColors)
            {
                selection.ReplaceAllIdenticalColors = replace;
                _refreshGameMenu = true;
            }
            GUILayout.Label("Face visée : peins un côté, retourne-toi et peins l'autre d'une autre couleur.", _muted);

            // Finish
            SectionHeader("Finition (portes, machines, pièces)");
            for (int start = 0; start < QuickFinishes.Length; start += 4)
            {
                GUILayout.BeginHorizontal();
                for (int i = start; i < Mathf.Min(start + 4, QuickFinishes.Length); i++)
                {
                    bool active = PaintBrush.Finish.Equals(QuickFinishes[i].Finish);
                    if (GUILayout.Button(QuickFinishes[i].Name, active ? _selectedButton : _button))
                        SetFinish(QuickFinishes[i].Finish);
                }
                GUILayout.EndHorizontal();
            }
            PaintFinish finish = PaintBrush.Finish;
            float gloss = Slider("Brillance", finish.Enabled ? finish.GlossValue : 0.5f);
            float metal = Slider("Métal", finish.Enabled ? finish.MetalValue : 0f);
            float glow = Slider("Lumineux", finish.Enabled ? finish.GlowValue : 0f);
            if (finish.Enabled
                ? (!Mathf.Approximately(gloss, finish.GlossValue) || !Mathf.Approximately(metal, finish.MetalValue) || !Mathf.Approximately(glow, finish.GlowValue))
                : (!Mathf.Approximately(gloss, 0.5f) || metal > 0f || glow > 0f))
            {
                SetFinish(PaintFinish.Of(gloss, metal, glow));
            }
            GUILayout.Label("Les murs et sols gardent leur aspect : le jeu ne gère que leur couleur.", _muted);

            // Presets
            SectionHeader("Préréglages");
            GUILayout.BeginHorizontal();
            _presetName = GUILayout.TextField(_presetName, 24, _field);
            if (GUILayout.Button("Enregistrer", _button, GUILayout.Width(110f)))
            {
                PaintBrush.AddPreset(_presetName);
                _presetName = "";
            }
            GUILayout.EndHorizontal();

            _presetScroll = GUILayout.BeginScrollView(_presetScroll, GUILayout.ExpandHeight(true));
            PaintPreset toRemove = null;
            foreach (PaintPreset preset in PaintBrush.Presets)
            {
                GUILayout.BeginHorizontal(_box);
                Rect presetSwatch = GUILayoutUtility.GetRect(30f, 30f, GUILayout.Width(30f), GUILayout.Height(30f));
                DrawSwatch(presetSwatch, preset.Color, preset.Finish);
                if (GUILayout.Button(preset.name + "\n" + FinishText(preset.Finish), _button, GUILayout.Height(36f)))
                {
                    PaintBrush.Apply(preset);
                    _refreshGameMenu = true;
                }
                if (GUILayout.Button("X", _button, GUILayout.Width(30f), GUILayout.Height(36f)))
                    toRemove = preset;
                GUILayout.EndHorizontal();
            }
            if (toRemove != null)
                PaintBrush.RemovePreset(toRemove);
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private static void SetColor(IPlayerPaintToolSelectionTracker selection, Color color)
        {
            selection.SelectedPaintColor = color;
            _hexInput = "#" + ColorUtility.ToHtmlStringRGB(color);
            _refreshGameMenu = true;
        }

        private static void SetBothSides(IPlayerPaintToolSelectionTracker selection, bool bothSides)
        {
            selection.PaintBothSides = bothSides;
            _refreshGameMenu = true;
        }

        private static void SetFinish(PaintFinish finish)
        {
            PaintBrush.Finish = finish;
            _finishDirty = true;
        }

        private static float Slider(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _label, GUILayout.Width(90f));
            value = GUILayout.HorizontalSlider(value, 0f, 1f, GUILayout.ExpandWidth(true));
            GUILayout.Label(Mathf.RoundToInt(value * 100f) + "%", _label, GUILayout.Width(44f));
            GUILayout.EndHorizontal();
            return value;
        }

        private static void SectionHeader(string text)
        {
            GUILayout.Space(10f);
            Rect line = GUILayoutUtility.GetRect(10f, 1f, GUILayout.ExpandWidth(true), GUILayout.Height(1f));
            Fill(line, Section);
            GUILayout.Label(text.ToUpperInvariant(), _title);
        }

        // Swatch with a gloss highlight, a metal gradient or a glow ring depending on the finish
        private static void DrawSwatch(Rect rect, Color color, PaintFinish finish)
        {
            Fill(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), finish.Enabled && finish.Glow > 0 ? color : Section);
            Fill(rect, color);
            if (!finish.Enabled)
                return;
            if (finish.Metal > 0)
                Fill(new Rect(rect.x, rect.y + rect.height * 0.5f, rect.width, rect.height * 0.5f), new Color(0f, 0f, 0f, 0.35f * finish.MetalValue));
            if (finish.Gloss > 0)
                Fill(new Rect(rect.x + rect.width * 0.12f, rect.y + rect.height * 0.12f, rect.width * 0.35f, rect.height * 0.12f), new Color(1f, 1f, 1f, 0.75f * finish.GlossValue));
        }

        private static string FinishText(PaintFinish finish)
        {
            if (!finish.Enabled)
                return "Finition d'origine";
            string text = $"Brillance {Mathf.RoundToInt(finish.GlossValue * 100f)}% · Métal {Mathf.RoundToInt(finish.MetalValue * 100f)}%";
            if (finish.Glow > 0)
                text += $" · Lumineux {Mathf.RoundToInt(finish.GlowValue * 100f)}%";
            return text;
        }

        private static string TargetText()
        {
            if (PaintToolPatches.HoveringHull)
                return "Cible : mur / sol (couleur seulement)";
            if (PaintToolPatches.HoveredPart.HasValue)
                return "Cible : pièce (couleur + finition)";
            return "Cible : aucune";
        }

        private static string Hex(string text)
        {
            text = text.Trim();
            return text.StartsWith("#") ? text : "#" + text;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _white);
            GUI.color = previous;
        }

        private static void EnsureStyles()
        {
            if (_white == null)
            {
                _white = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                _white.SetPixel(0, 0, Color.white);
                _white.Apply();
            }
            if (_title != null)
                return;

            _title = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            _title.normal.textColor = Accent;
            _label = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            _label.normal.textColor = TextColor;
            _muted = new GUIStyle(_label) { fontSize = 11 };
            _muted.normal.textColor = MutedText;
            _big = new GUIStyle(_label) { fontSize = 20, fontStyle = FontStyle.Bold };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 12, wordWrap = true };
            _button.normal.background = Solid(new Color(0.17f, 0.18f, 0.22f));
            _button.hover.background = Solid(new Color(0.24f, 0.26f, 0.31f));
            _button.active.background = Solid(new Color(0.3f, 0.32f, 0.38f));
            _button.normal.textColor = _button.hover.textColor = _button.active.textColor = TextColor;
            _selectedButton = new GUIStyle(_button) { fontStyle = FontStyle.Bold };
            _selectedButton.normal.background = _selectedButton.hover.background = Solid(Accent * 0.85f);
            _selectedButton.normal.textColor = _selectedButton.hover.textColor = Color.white;
            _field = new GUIStyle(GUI.skin.textField) { fontSize = 13 };
            _field.normal.background = _field.focused.background = Solid(new Color(0.03f, 0.035f, 0.045f));
            _field.normal.textColor = _field.focused.textColor = TextColor;
            _box = new GUIStyle { padding = new RectOffset(4, 4, 3, 3), margin = new RectOffset(0, 0, 2, 2) };
            _box.normal.background = Solid(Section);
        }

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
