using Core.Utils;
using HarmonyLib;
using Ships.Interface.Model;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.Common;
using Ships.Interface.Services;
using Ships.Parts.Common.Model.Reactions;
using Ships.Visuals;
using StellarDriveDemoTF.Common;
using Tools.Common.Hover;
using Tools.PaintTool;
using UI.PaintMenu;
using UnityEngine;
using WorldTracking.Interface.Values;

namespace StellarDriveDemoTF.Paint
{
    /// <summary>
    /// Extends the game's paint tool: when it is not aiming at a wall or floor, it paints the
    /// floating part under the crosshair (door, machine, seat, ...) instead.
    /// </summary>
    [HarmonyPatch(typeof(PaintTool))]
    internal static class PaintToolPatches
    {
        private static readonly AccessTools.FieldRef<PaintTool, HullHover> HoveredHull =
            AccessTools.FieldRefAccess<PaintTool, HullHover>("hoveredObject");
        private static readonly AccessTools.FieldRef<PaintTool, float> RayDistance =
            AccessTools.FieldRefAccess<PaintTool, float>("rayCastDistance");
        private static readonly AccessTools.FieldRef<PaintTool, int> LayerMask =
            AccessTools.FieldRefAccess<PaintTool, int>("_layerMask");
        private static readonly AccessTools.FieldRef<PaintTool, bool> IsSpraying =
            AccessTools.FieldRefAccess<PaintTool, bool>("_isSpraying");

        private static PartKey? _hoveredPart;
        private static PartKey? _lastPainted;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PaintTool.UpdateChecks))]
        private static void AfterUpdateChecks(PaintTool __instance, CameraPhysicsData cameraPhysics)
        {
            _hoveredPart = null;
            HoveringHull = HoveredHull(__instance) != null;
            if (!Settings.PaintAllParts.Value || HoveringHull)
                return;

            if (!ClientScene.PhysicsScene.Raycast(cameraPhysics.CameraPos, cameraPhysics.Direction, out RaycastHit hit,
                    RayDistance(__instance), LayerMask(__instance), QueryTriggerInteraction.Ignore))
                return;
            if (!ShipPartsTagDetection.ColliderIsFloatingPart(hit.collider))
                return;

            var ship = hit.collider.GetComponentInParent<IShipPartTargeter>();
            var part = hit.collider.GetComponentInParent<IShipFloatingPartId>();
            if (ship == null || part == null)
                return;

            var key = new PartKey(ship.ShipId, part.Id);
            _hoveredPart = key;

            if (IsSpraying(__instance) && !key.Equals(_lastPainted))
            {
                var selection = GameServices.PaintSelection;
                if (selection == null)
                    return;
                PaintNet.RequestPaint(key, PaintBrush.Current, selection.ReplaceAllIdenticalColors);
                _lastPainted = key;
            }
        }

        // A new click may repaint the part painted by the previous one
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PaintTool.PrimaryActionBegin))]
        private static void AfterPrimaryActionBegin() => _lastPainted = null;

        // Middle click picks the paint (color and finish) of the hovered part
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PaintTool.TertiaryActionBegin))]
        private static void AfterTertiaryActionBegin(PaintTool __instance)
        {
            if (HoveredHull(__instance) != null || !_hoveredPart.HasValue)
                return;
            var selection = GameServices.PaintSelection;
            if (selection != null && PaintNet.Client.TryGet(_hoveredPart.Value, out PaintData paint))
            {
                selection.SelectedPaintColor = paint.Color;
                PaintBrush.Finish = paint.Finish;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PaintTool.EnableTool))]
        private static void AfterEnableTool()
        {
            PaintBrush.EnsureLoaded();
            PaintHud.ToolActive = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PaintTool.DisableTool))]
        private static void AfterDisableTool()
        {
            _hoveredPart = null;
            _lastPainted = null;
            PaintHud.ToolActive = false;
            PaintBrush.Save();
        }

        /// <summary>What the paint tool is aiming at, for the HUD.</summary>
        public static PartKey? HoveredPart => _hoveredPart;
        public static bool HoveringHull { get; private set; }
    }

    /// <summary>Tracks whether the game's paint menu (right click) is open, so the HUD can show its full panel.</summary>
    [HarmonyPatch(typeof(PaintMenuActivator))]
    internal static class PaintMenuActivatorPatch
    {
        private static readonly AccessTools.FieldRef<PaintMenuActivator, PaintMenu> GameMenu =
            AccessTools.FieldRefAccess<PaintMenuActivator, PaintMenu>("_paintUI");

        // The game's menu stays open (it frees the cursor and pauses the tool) but is hidden:
        // the TF panel replaces it
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PaintMenuActivator.Open))]
        private static void AfterOpen(PaintMenuActivator __instance)
        {
            PaintHud.MenuOpen = true;
            PaintMenu menu = GameMenu(__instance);
            if (menu != null)
                menu.gameObject.SetActive(false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PaintMenuActivator.Close))]
        private static void AfterClose() => PaintHud.MenuOpen = false;
    }

    /// <summary>Paints a part's visual as soon as it is created (ship coming into view, part placed).</summary>
    [HarmonyPatch(typeof(ShipPartVisuals), nameof(ShipPartVisuals.AddStatefulPart))]
    internal static class ShipPartVisualsPatch
    {
        private static void Postfix(ShipPartVisuals __instance, StatefulPart part)
        {
            var key = new PartKey(__instance.ShipId, part.Id);
            if (!PaintNet.Client.TryGet(key, out _))
                return;
            if (__instance.TryGetFloatingPartVisual(part.Id, out IShipPartVisual visual) && visual != null)
                PartTint.Apply(visual.GameObject, key);
        }
    }

    /// <summary>
    /// A removed part's id can be reused by the next part placed on that ship, so its color must go.
    /// ShipState.RemoveStatefulPart only runs for real removals (recycling, destruction), on the
    /// server and when clients receive the removal.
    /// </summary>
    [HarmonyPatch(typeof(ShipState), nameof(ShipState.RemoveStatefulPart))]
    internal static class ShipStateRemovePartPatch
    {
        private static void Prefix(ShipState __instance, ushort id)
        {
            PaintNet.ForgetPart(new PartKey(__instance.Id, id));
        }
    }
}
