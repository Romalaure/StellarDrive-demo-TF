using System.Collections.Generic;
using System.Linq;
using Core.Services;
using FishNet.Connection;
using HarmonyLib;
using Items.Model;
using Players;
using Players.Inventory;
using Ships.Interface.Settings;
using StellarDriveDemoTF.Common;
using StellarDriveDemoTF.Paint;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StellarDriveDemoTF.Devices
{
    /// <summary>
    /// The tool rack: a wall panel holding a paint gun and a schematic tablet. Look at it and press
    /// the use key to take either one as an item in your inventory (one of each per player).
    /// </summary>
    internal static class ToolRack
    {
        public const ushort RackId = 7175;

        private static readonly Vector3 BoundsCenter = new Vector3(0f, 0.04f, 0f);
        private static readonly Vector3 BoundsSize = new Vector3(0.9f, 0.08f, 0.6f);

        public static void Register()
        {
            Lights.LampParts.RegisterDonorPart(RackId, TFTab.DevicesRow, Configure, TFTab.ObjectsName);
            TFMod.Log.Msg("registered the tool rack");
        }

        /// <summary>Network handler; installed whether or not the part could be registered.</summary>
        public static void Install()
        {
            TFNet.OnServer(TFMessageKind.GiveTool, ServerGive);
        }

        private static void Configure(PartSettings settings, GameObject prefab)
        {
            Transform visuals = DeviceModels.Build(settings, prefab, "Râtelier à outils",
                "Panneau à fixer au mur : regarde-le et appuie sur la touche d'utilisation (T) pour prendre un pistolet à peinture ou une tablette schématique.",
                4f, new[] { (101u, 2), (110u, 2) }, BoundsCenter, BoundsSize, Shapes(), Materials());
            UsablePart.AddBox(visuals, BoundsCenter, BoundsSize + new Vector3(0f, 0.25f, 0f));
            visuals.gameObject.AddComponent<ToolRackVisuals>();
        }

        private static Dictionary<string, DeviceMaterial> Materials() => new Dictionary<string, DeviceMaterial>
        {
            ["TF_Rack_Board"] = new DeviceMaterial(new Color(0.3f, 0.33f, 0.36f), 0.3f, 0.4f),
            ["TF_NoPaint_RackMetal"] = new DeviceMaterial(new Color(0.75f, 0.76f, 0.78f), 1f, 0.75f),
            ["TF_NoPaint_RackGun"] = new DeviceMaterial(new Color(0.95f, 0.45f, 0.1f), 0.2f, 0.6f),
            ["TF_NoPaint_RackTablet"] = new DeviceMaterial(new Color(0.08f, 0.09f, 0.11f), 0.1f, 0.8f),
            ["TF_NoPaint_RackScreen"] = new DeviceMaterial(new Color(0.3f, 0.75f, 1f), 0f, 0.9f, new Color(0.25f, 0.65f, 1f) * 2f)
        };

        // Lies on its mounting surface (+y out of it); seen on a wall, x is across and z is up
        internal static Dictionary<string, MeshBuilder> Shapes()
        {
            var board = new MeshBuilder();
            var metal = new MeshBuilder();
            var gun = new MeshBuilder();
            var tablet = new MeshBuilder();
            var screen = new MeshBuilder();
            board.Box(new Vector3(0f, 0.01f, 0f), new Vector3(0.9f, 0.02f, 0.6f));
            // Hooks
            foreach (float x in new[] { -0.3f, -0.15f, 0.1f, 0.3f })
                metal.Cylinder(new Vector3(x, 0.02f, 0.12f), new Vector3(x, 0.07f, 0.12f), 0.006f, 0.006f, 6);
            // Paint gun on the left: body, nozzle, cup and grip
            gun.Box(new Vector3(-0.22f, 0.06f, 0.05f), new Vector3(0.2f, 0.05f, 0.06f));
            metal.Cylinder(new Vector3(-0.32f, 0.06f, 0.05f), new Vector3(-0.38f, 0.06f, 0.05f), 0.012f, 0.006f, 8);
            gun.Cylinder(new Vector3(-0.2f, 0.06f, 0.08f), new Vector3(-0.2f, 0.06f, 0.17f), 0.035f, 0.03f, 12);
            gun.Box(new Vector3(-0.16f, 0.055f, -0.03f), new Vector3(0.035f, 0.04f, 0.12f));
            // Tablet on the right
            tablet.Box(new Vector3(0.2f, 0.035f, 0f), new Vector3(0.26f, 0.015f, 0.36f));
            screen.Box(new Vector3(0.2f, 0.044f, 0.005f), new Vector3(0.22f, 0.004f, 0.3f));
            return new Dictionary<string, MeshBuilder>
            {
                ["TF_Rack_Board"] = board,
                ["TF_NoPaint_RackMetal"] = metal,
                ["TF_NoPaint_RackGun"] = gun,
                ["TF_NoPaint_RackTablet"] = tablet,
                ["TF_NoPaint_RackScreen"] = screen
            };
        }

        public static void RequestTool(uint itemId) => TFNet.SendToServer(TFMessageKind.GiveTool, itemId, 0, "");

        private static void ServerGive(NetworkConnection sender, TFMessageKind kind, uint itemId, ushort b, string text)
        {
            if (sender == null)
                return;
            if (itemId == SchematicTablet.ItemId)
            {
                TrackedPlayerServer player = GameServices.PlayersServer?.GetTrackedPlayerFromConnectionId(sender.ClientId);
                if (player == null)
                    return;
                var drop = new ItemDrop { ItemId = SchematicTablet.ItemId, Quantity = 1 };
                if (!player.HasItemsInInventory(new[] { drop }))
                    player.AddItemToInventory(drop);
            }
            else
            {
                PaintToolGiver.ServerGive(sender);
            }
        }
    }

    internal sealed class ToolRackVisuals : UsablePart
    {
        public override string Label => "Râtelier à outils";

        public override void Use() => ToolRackMenu.Show();
    }

    internal static class ToolRackMenu
    {
        private static bool _open;

        public static void Show()
        {
            _open = true;
            ModMenu.Open(() => _open = false);
        }

        public static void Draw()
        {
            if (!_open || !ModMenu.IsOpen)
                return;
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 2.5f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            Rect area = DeviceUi.Window(Screen.width / scale, Screen.height / scale, 480f, 330f, "RÂTELIER À OUTILS");
            GUILayout.BeginArea(area);
            GUILayout.Label("Prends un outil : il va dans ton inventaire (un de chaque par joueur).", DeviceUi.Text);
            GUILayout.Space(10f);
            if (GUILayout.Button("Pistolet à peinture  (peint murs, sols et pièces)", DeviceUi.Button, GUILayout.Height(44f)))
            {
                ToolRack.RequestTool(PaintToolGiver.FindPaintToolItem()?.id ?? 3u);
                ModMenu.Close();
            }
            GUILayout.Space(6f);
            if (GUILayout.Button("Tablette schématique  (copier et construire des vaisseaux)", DeviceUi.Button, GUILayout.Height(44f)))
            {
                ToolRack.RequestTool(SchematicTablet.ItemId);
                ModMenu.Close();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Fermer (Échap)", DeviceUi.Button, GUILayout.Height(34f)))
                ModMenu.Close();
            GUILayout.EndArea();
            GUI.matrix = previous;
        }
    }

    /// <summary>
    /// The schematic tablet: a new inventory item. Held in the hand (selected in the toolbar), a
    /// left click opens the schematics window.
    /// </summary>
    internal static class SchematicTablet
    {
        public const uint ItemId = 7201;

        private static ItemSettings _item;
        private static GameObject _holder;
        private static GUIStyle _hint;
        private static bool _holding;

        private static readonly AccessTools.FieldRef<ToolBelt, Core.Values.IntValue> SelectedTool =
            AccessTools.FieldRefAccess<ToolBelt, Core.Values.IntValue>("currentSelectedId");

        public static ItemSettings Item
        {
            get
            {
                if (_item == null)
                    _item = Create();
                return _item;
            }
        }

        private static ItemSettings Create()
        {
            if (_holder == null)
            {
                // Inactive holder: the model is a template, cloned for thumbnails and dropped items
                _holder = new GameObject("TF_ItemModels");
                _holder.SetActive(false);
                Object.DontDestroyOnLoad(_holder);
            }
            var model = new GameObject("TF_SchematicTablet");
            model.transform.SetParent(_holder.transform, false);
            var body = new MeshBuilder();
            var screen = new MeshBuilder();
            body.Box(new Vector3(0f, 0.008f, 0f), new Vector3(0.24f, 0.016f, 0.34f));
            screen.Box(new Vector3(0f, 0.0165f, 0.01f), new Vector3(0.21f, 0.002f, 0.28f));
            AddMesh(model, "Body", body, LitMaterials.Get("TF_TabletBody", new Color(0.1f, 0.11f, 0.13f), 0.2f, 0.8f));
            AddMesh(model, "Screen", screen, LitMaterials.Get("TF_TabletScreen", new Color(0.3f, 0.75f, 1f), 0f, 0.9f, new Color(0.25f, 0.65f, 1f) * 2f));

            var item = ScriptableObject.CreateInstance<ItemSettings>();
            item.name = "TF_SchematicTablet";
            item.id = ItemId;
            item.itemName = "Tablette schématique";
            item.mass = 1f;
            item.maxStackSize = 1;
            item.itemObject = model;
            return item;
        }

        private static void AddMesh(GameObject parent, string name, MeshBuilder shape, Material material)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = shape.Build("TF_Tablet_" + name);
            child.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        public static void Update()
        {
            _holding = IsHeld();
            if (_holding && !ModMenu.IsOpen && !Cameras.CameraTablet.Open && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                SchematicsMenu.Show();
        }

        private static bool IsHeld()
        {
            if (GameServices.ShipsClient == null)
                return false;
            ToolBelt belt = Object.FindFirstObjectByType<ToolBelt>();
            var toolbar = ServiceLocator.GetService<PlayerToolbarProvider>();
            if (belt == null || toolbar == null)
                return false;
            int selected = SelectedTool(belt).Get();
            if (selected < 1)
                return false;
            InventorySlot slot = toolbar.GetSlot((byte)(selected - 1));
            return slot.HasItem && slot.ItemId == ItemId;
        }

        public static void Draw()
        {
            if (!_holding || ModMenu.IsOpen)
                return;
            if (_hint == null)
            {
                _hint = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _hint.normal.textColor = new Color(0.95f, 0.96f, 0.98f);
            }
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 2.5f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale, height = Screen.height / scale;
            var rect = new Rect(width / 2f - 200f, height - 150f, 400f, 34f);
            GUI.Box(rect, GUIContent.none, UiTextures.RoundedStyle(new Color(0.05f, 0.06f, 0.08f, 0.82f), 10));
            GUI.Label(rect, "Tablette schématique  ·  clic gauche pour l'ouvrir", _hint);
            GUI.matrix = previous;
        }
    }

    /// <summary>Adds the schematic tablet to the game's item list, on every machine with the mod.</summary>
    [HarmonyPatch(typeof(ItemSettingsList), "ResetDict")]
    internal static class SchematicTabletItemPatch
    {
        private static void Prefix(ref ItemSettings[] ___items)
        {
            if (___items == null || ___items.Any(i => i != null && i.id == SchematicTablet.ItemId))
                return;
            ___items = ___items.Concat(new[] { SchematicTablet.Item }).ToArray();
        }
    }
}
