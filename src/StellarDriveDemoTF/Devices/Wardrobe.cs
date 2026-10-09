using System.Collections.Generic;
using System.Globalization;
using FishNet.Connection;
using HarmonyLib;
using Players;
using Players.Services;
using Ships.Interface.Settings;
using StellarDriveDemoTF.Common;
using UnityEngine;

namespace StellarDriveDemoTF.Devices
{
    /// <summary>
    /// The wardrobe: a changing booth. Look at it and press the use key to pick your suit's main
    /// color and your helmet and backpack color. Colors are saved in your settings, sent to the
    /// host and shown on your character to every player who has the mod.
    /// </summary>
    internal static class Wardrobe
    {
        public const ushort WardrobeId = 7171;

        private static readonly Vector3 BoundsCenter = new Vector3(0f, 1.15f, 0f);
        private static readonly Vector3 BoundsSize = new Vector3(1.2f, 2.3f, 1.1f);

        public static void Register()
        {
            Lights.LampParts.RegisterDonorPart(WardrobeId, TFTab.DevicesRow, Configure);
            TFMod.Log.Msg("registered the wardrobe");
        }

        private static void Configure(PartSettings settings, GameObject prefab)
        {
            Transform visuals = DeviceModels.Build(settings, prefab, "Cabine vestimentaire",
                "Cabine d'essayage : regarde-la et appuie sur la touche d'utilisation (T) pour changer la couleur principale de ta combinaison et la couleur secondaire (casque et sac). Les autres joueurs voient tes couleurs.",
                40f, new[] { (110u, 8), (101u, 2), (102u, 2) }, BoundsCenter, BoundsSize, Shapes(), Materials());
            DeviceModels.FlattenColliders(prefab, new Vector3(1.1f, 0.06f, 1.0f));
            UsablePart.AddBox(visuals, BoundsCenter, BoundsSize);
            visuals.gameObject.AddComponent<WardrobeVisuals>();
        }

        private static Dictionary<string, DeviceMaterial> Materials() => new Dictionary<string, DeviceMaterial>
        {
            ["TF_Wardrobe_Body"] = new DeviceMaterial(new Color(0.62f, 0.44f, 0.28f), 0f, 0.35f),
            ["TF_NoPaint_WardrobeCurtain"] = new DeviceMaterial(new Color(0.55f, 0.12f, 0.14f), 0f, 0.15f),
            ["TF_NoPaint_WardrobeMetal"] = new DeviceMaterial(new Color(0.75f, 0.76f, 0.78f), 1f, 0.75f),
            ["TF_NoPaint_WardrobeMirror"] = new DeviceMaterial(new Color(0.75f, 0.82f, 0.88f), 1f, 0.97f),
            ["TF_NoPaint_WardrobeLight"] = new DeviceMaterial(new Color(1f, 0.92f, 0.75f), 0f, 0.8f, new Color(1f, 0.85f, 0.6f) * 2.5f)
        };

        internal static Dictionary<string, MeshBuilder> Shapes()
        {
            var body = new MeshBuilder();
            var curtain = new MeshBuilder();
            var metal = new MeshBuilder();
            var mirror = new MeshBuilder();
            var light = new MeshBuilder();
            const float w = 1.1f, h = 2.25f, d = 1.0f, t = 0.05f;

            // Floor, back, sides and roof; the front (-z) is the curtain
            body.Box(new Vector3(0f, 0.03f, 0f), new Vector3(w, 0.06f, d));
            body.Box(new Vector3(0f, h / 2f, d / 2f - t / 2f), new Vector3(w, h, t));
            body.Box(new Vector3(-w / 2f + t / 2f, h / 2f, 0f), new Vector3(t, h, d));
            body.Box(new Vector3(w / 2f - t / 2f, h / 2f, 0f), new Vector3(t, h, d));
            body.Box(new Vector3(0f, h + 0.03f, 0f), new Vector3(w + 0.06f, 0.06f, d + 0.06f));
            // Crown molding along the front
            body.Box(new Vector3(0f, h - 0.06f, -d / 2f + 0.03f), new Vector3(w, 0.12f, 0.06f));

            // Curtain rod and a curtain pulled to the left, in folds
            metal.Cylinder(new Vector3(-w / 2f + t, h - 0.16f, -d / 2f + 0.09f), new Vector3(w / 2f - t, h - 0.16f, -d / 2f + 0.09f), 0.015f, 0.015f, 8);
            for (int i = 0; i < 5; i++)
            {
                float x = -w / 2f + t + 0.05f + i * 0.075f;
                float z = -d / 2f + 0.09f + (i % 2 == 0 ? -0.025f : 0.025f);
                curtain.Box(new Vector3(x, (h - 0.2f) / 2f + 0.08f, z), new Vector3(0.08f, h - 0.3f, 0.025f));
            }
            // Mirror on the back wall, in a metal frame
            metal.Box(new Vector3(0f, 1.25f, d / 2f - t - 0.01f), new Vector3(0.5f, 1.3f, 0.02f));
            mirror.Box(new Vector3(0f, 1.25f, d / 2f - t - 0.025f), new Vector3(0.44f, 1.24f, 0.01f));
            // Hook with a hanger on the right wall
            metal.Box(new Vector3(w / 2f - t - 0.03f, 1.75f, 0.1f), new Vector3(0.06f, 0.03f, 0.03f));
            metal.Cylinder(new Vector3(w / 2f - t - 0.06f, 1.62f, -0.1f), new Vector3(w / 2f - t - 0.06f, 1.62f, 0.3f), 0.008f, 0.008f, 6);
            // Warm ceiling light
            light.Box(new Vector3(0f, h - 0.015f, 0f), new Vector3(0.4f, 0.02f, 0.2f));

            return new Dictionary<string, MeshBuilder>
            {
                ["TF_Wardrobe_Body"] = body,
                ["TF_NoPaint_WardrobeCurtain"] = curtain,
                ["TF_NoPaint_WardrobeMetal"] = metal,
                ["TF_NoPaint_WardrobeMirror"] = mirror,
                ["TF_NoPaint_WardrobeLight"] = light
            };
        }
    }

    internal sealed class WardrobeVisuals : UsablePart
    {
        public override string Label => "Cabine vestimentaire";

        public override void Use() => WardrobeMenu.Show();
    }

    /// <summary>The color picker of the wardrobe.</summary>
    internal static class WardrobeMenu
    {
        private static readonly Color[] Palette =
        {
            new Color(0.93f, 0.94f, 0.95f), new Color(0.55f, 0.57f, 0.6f), new Color(0.16f, 0.17f, 0.19f), new Color(0.05f, 0.05f, 0.06f),
            new Color(0.85f, 0.15f, 0.15f), new Color(0.98f, 0.45f, 0.1f), new Color(0.98f, 0.8f, 0.15f), new Color(0.55f, 0.8f, 0.2f),
            new Color(0.15f, 0.6f, 0.3f), new Color(0.3f, 0.4f, 0.2f), new Color(0.15f, 0.75f, 0.8f), new Color(0.2f, 0.45f, 0.9f),
            new Color(0.1f, 0.18f, 0.45f), new Color(0.5f, 0.25f, 0.8f), new Color(0.9f, 0.35f, 0.65f), new Color(0.5f, 0.32f, 0.2f),
            new Color(0.8f, 0.68f, 0.5f), new Color(0.45f, 0.08f, 0.12f)
        };

        private static bool _open;
        private static string _primaryText, _secondaryText;

        public static void Show()
        {
            _open = true;
            _primaryText = Settings.OutfitPrimary.Value;
            _secondaryText = Settings.OutfitSecondary.Value;
            ModMenu.Open(() => _open = false);
        }

        public static void Draw()
        {
            if (!_open || !ModMenu.IsOpen)
                return;
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 2.5f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale, height = Screen.height / scale;

            Rect area = DeviceUi.Window(width, height, 560f, 560f, "CABINE VESTIMENTAIRE");
            GUILayout.BeginArea(area);
            Section("Couleur principale (combinaison)", ref _primaryText, Settings.OutfitPrimary);
            GUILayout.Space(14f);
            Section("Couleur secondaire (casque et sac)", ref _secondaryText, Settings.OutfitSecondary);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Les couleurs teintent ton personnage tel que les autres joueurs le voient.", DeviceUi.Muted);
            GUILayout.Space(6f);
            if (GUILayout.Button("Fermer (Échap)", DeviceUi.Button, GUILayout.Height(34f)))
                ModMenu.Close();
            GUILayout.EndArea();

            GUI.matrix = previous;
        }

        private static void Section(string title, ref string text, MelonLoader.MelonPreferences_Entry<string> entry)
        {
            GUILayout.Label(title, DeviceUi.Text);
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            Rect preview = GUILayoutUtility.GetRect(56f, 56f, GUILayout.Width(56f), GUILayout.Height(56f));
            bool hasColor = Outfits.TryParse(entry.Value, out Color current);
            DeviceUi.Fill(preview, hasColor ? current : new Color(0.3f, 0.31f, 0.34f));
            if (!hasColor)
                GUI.Label(preview, "  jeu", DeviceUi.Muted);
            GUILayout.Space(10f);
            GUILayout.BeginVertical();
            for (int row = 0; row < 2; row++)
            {
                GUILayout.BeginHorizontal();
                for (int i = row * 9; i < row * 9 + 9; i++)
                {
                    Rect swatch = GUILayoutUtility.GetRect(36f, 26f, GUILayout.Width(36f), GUILayout.Height(26f));
                    DeviceUi.Fill(new Rect(swatch.x + 2f, swatch.y + 2f, swatch.width - 4f, swatch.height - 4f), Palette[i]);
                    if (GUI.Button(swatch, GUIContent.none, GUIStyle.none))
                    {
                        text = Outfits.ToHex(Palette[i]);
                        Set(entry, text);
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("#", DeviceUi.Text, GUILayout.Width(12f));
            string edited = GUILayout.TextField(text ?? "", 6, DeviceUi.Field, GUILayout.Width(110f), GUILayout.Height(32f));
            if (edited != text)
            {
                text = edited;
                if (Outfits.TryParse(text, out _))
                    Set(entry, text.ToUpperInvariant());
            }
            GUILayout.Space(8f);
            if (GUILayout.Button("Couleur du jeu", DeviceUi.Button, GUILayout.Height(32f)))
            {
                text = "";
                Set(entry, "");
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static void Set(MelonLoader.MelonPreferences_Entry<string> entry, string value)
        {
            if (entry.Value == value)
                return;
            entry.Value = value;
            MelonLoader.MelonPreferences.Save();
            Outfits.SendLocal();
        }
    }

    /// <summary>
    /// Player colors: each player's choice goes to the host, which tells everyone; every client
    /// tints the matching character model. The suit body takes the main color, the helmet shell
    /// and backpack the second one (the visor keeps its look).
    /// </summary>
    internal static class Outfits
    {
        private static readonly Dictionary<uint, string> ServerOutfits = new Dictionary<uint, string>();
        private static readonly Dictionary<uint, string> ClientOutfits = new Dictionary<uint, string>();
        private static readonly Dictionary<int, string> Applied = new Dictionary<int, string>();
        private static readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static readonly AccessTools.FieldRef<PlayersClientTracker, List<TrackedPlayerOtherClient>> OtherPlayers =
            AccessTools.FieldRefAccess<PlayersClientTracker, List<TrackedPlayerOtherClient>>("_otherPlayers");
        private static readonly AccessTools.FieldRef<TrackedPlayerLocalClient, GameObject> LocalVisual =
            AccessTools.FieldRefAccess<TrackedPlayerLocalClient, GameObject>("_localVisualObject");

        private static float _nextApply;

        public static void Install()
        {
            TFNet.OnServer(TFMessageKind.Outfit, OnServerOutfit);
            TFNet.OnClient(TFMessageKind.Outfit, (kind, playerId, b, text) => ClientOutfits[playerId] = text);
            TFNet.SyncRequested += connection =>
            {
                foreach (KeyValuePair<uint, string> outfit in ServerOutfits)
                    TFNet.SendTo(connection, TFMessageKind.Outfit, outfit.Key, 0, outfit.Value);
            };
            TFNet.Synced += SendLocal;
            TFNet.Disconnected += () =>
            {
                ServerOutfits.Clear();
                ClientOutfits.Clear();
                Applied.Clear();
            };
        }

        public static void SendLocal()
        {
            string primary = TryParse(Settings.OutfitPrimary.Value, out Color a) ? ToHex(a) : "-";
            string secondary = TryParse(Settings.OutfitSecondary.Value, out Color b) ? ToHex(b) : "-";
            TFNet.SendToServer(TFMessageKind.Outfit, 0, 0, primary + " " + secondary);
        }

        private static void OnServerOutfit(NetworkConnection sender, TFMessageKind kind, uint a, ushort b, string text)
        {
            if (sender == null)
                return;
            TrackedPlayerServer player = GameServices.PlayersServer?.GetTrackedPlayerFromConnectionId(sender.ClientId);
            if (player == null || text.Length > 20)
                return;
            ServerOutfits[player.Id] = text;
            TFNet.SendToAll(TFMessageKind.Outfit, player.Id, 0, text);
        }

        public static void Update()
        {
            PlayersClientTracker players = GameServices.PlayersClient;
            if (players == null || players.LocalPlayer == null)
                return;
            if (Time.unscaledTime < _nextApply)
                return;
            _nextApply = Time.unscaledTime + 0.5f;

            TrackedPlayerLocalClient local = players.LocalPlayer;
            if (ClientOutfits.TryGetValue(local.Id, out string own))
                Apply(LocalVisual(local), own);
            List<TrackedPlayerOtherClient> others = OtherPlayers(players);
            if (others == null)
                return;
            foreach (TrackedPlayerOtherClient other in others)
            {
                if (other != null && ClientOutfits.TryGetValue(other.Id, out string outfit))
                    Apply(other._visualObject, outfit);
            }
        }

        private static void Apply(GameObject visual, string outfit)
        {
            if (visual == null)
                return;
            int id = visual.GetInstanceID();
            if (Applied.TryGetValue(id, out string done) && done == outfit)
                return;
            Applied[id] = outfit;

            string[] parts = outfit.Split(' ');
            bool hasPrimary = TryParse(parts.Length > 0 ? parts[0] : "", out Color primary);
            bool hasSecondary = TryParse(parts.Length > 1 ? parts[1] : "", out Color secondary);

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is SkinnedMeshRenderer || renderer is MeshRenderer))
                    continue;
                string name = renderer.gameObject.name.ToLowerInvariant();
                if (name.Contains("visor") || name.Contains("cable") || name.Contains("glass") || name.StartsWith("tf_"))
                    continue;
                bool secondaryPart = name.Contains("helmet") || name.Contains("back") || name.Contains("pack") || name.Contains("tank");
                bool tinted = secondaryPart ? hasSecondary : hasPrimary;
                Color color = secondaryPart ? secondary : primary;
                renderer.GetPropertyBlock(Block);
                Block.SetColor(ColorId, tinted ? color : Color.white);
                Block.SetColor(BaseColorId, tinted ? color : Color.white);
                renderer.SetPropertyBlock(Block);
            }
        }

        public static bool TryParse(string hex, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrWhiteSpace(hex))
                return false;
            hex = hex.Trim().TrimStart('#');
            if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value))
                return false;
            color = new Color(((value >> 16) & 255) / 255f, ((value >> 8) & 255) / 255f, (value & 255) / 255f);
            return true;
        }

        public static string ToHex(Color color) =>
            $"{Mathf.RoundToInt(color.r * 255f):X2}{Mathf.RoundToInt(color.g * 255f):X2}{Mathf.RoundToInt(color.b * 255f):X2}";
    }
}
