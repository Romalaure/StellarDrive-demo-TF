using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Research;
using SDModKit.Game;
using Ships.Interface.Model;
using Ships.Interface.Model.Joints;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.State;
using Ships.Interface.Model.Placement;
using Ships.Interface.Model.State;
using Ships.Interface.Services;
using Ships.Interface.Settings;
using Ships.Joints;
using Ships.Parts.Common.Model.Reactions;
using Ships.Physics.Collision;
using StellarDriveDemoTF.Common;
using Tools.Build;
using UnityEngine;

namespace StellarDriveDemoTF.Devices
{
    /// <summary>
    /// Doors laid in the floor: trapdoors (the game's door, 2x2 or shrunk to 1x1) and horizontal
    /// docking doors (the game's docking door, docking downward or upward). They are the game's
    /// own parts, cloned and set to snap like floor tiles, so opening, closing, saving and syncing
    /// stay the game's.
    ///
    /// The game already lays floor parts flat (their forward axis is vertical), but a door's model
    /// grows from its corner (+x, +y) while a 2x2 floor part takes the tiles toward +x and +z of the
    /// ship. So each door's frame (the rotation and position the game reads from the part to place
    /// its model, colliders, interaction, magnet and docking joint) is recentred on its tiles, and
    /// docking doors are turned to face down or up whatever way the player looked when placing.
    /// The build preview gets the same frame.
    /// </summary>
    internal static class HullDoors
    {
        public const ushort Trapdoor2 = 7181;
        public const ushort Trapdoor1 = 7182;
        public const ushort DockDown = 7183;
        public const ushort DockUp = 7184;

        private static readonly Vector3 Corner2 = new Vector3(0.5f, 0.5f, 0f);

        public static bool IsFloorDoor(ushort id) => id >= Trapdoor2 && id <= DockUp;

        public static bool IsDock(ushort id) => id == DockDown || id == DockUp;

        public static void Register()
        {
            Add(Trapdoor2, "Door", "Trappe 2×2",
                "Porte posée dans le sol, sur 2×2 cases : retire 4 dalles de sol et pose la trappe à leur place. S'ouvre et se ferme comme une porte.",
                2);
            Add(Trapdoor1, "Door", "Trappe 1×1",
                "Petite porte posée dans le sol, sur une seule case : retire une dalle de sol et pose la trappe à sa place. S'ouvre et se ferme comme une porte.",
                1);
            Add(DockDown, "DockingDoor", "Porte d'amarrage (sol, vers le bas)",
                "Porte d'amarrage posée dans le sol, sur 2×2 cases, qui s'amarre vers le BAS : sous un vaisseau, elle accroche une porte d'amarrage tournée vers le haut. Mêmes fonctions que la porte d'amarrage du jeu.",
                2);
            Add(DockUp, "DockingDoor", "Porte d'amarrage (sol, vers le haut)",
                "Porte d'amarrage posée dans le sol, sur 2×2 cases, qui s'amarre vers le HAUT : sur le toit d'un vaisseau ou d'une base, elle accroche une porte d'amarrage tournée vers le bas. Mêmes fonctions que la porte d'amarrage du jeu.",
                2);
            TFMod.Log.Msg("registered trapdoors and horizontal docking doors");
        }

        private static void Add(ushort id, string donor, string label, string description, int tiles)
        {
            CustomParts.Register(new CustomPartDefinition
            {
                Id = id,
                Name = "TF_" + id,
                Donor = donor,
                BuildTab = TFTab.ObjectsName,
                BuildRow = TFTab.DoorsRow,
                Configure = (settings, prefab) => Configure(settings, prefab, label, description, tiles)
            });
        }

        private static void Configure(PartSettings settings, GameObject prefab, string label, string description, int tiles)
        {
            settings.fullLabel = label;
            settings.description = description;
            settings.localizedDescription = null;
            settings.snappingStyle = SnappingStyle.Floor;
            settings.size = new Vector3(tiles, 1f, tiles);
            if (settings is HullPartSettings hull)
            {
                hull.defaultForward = PartOrientation.Down;
                hull.defaultUp = PartOrientation.Forward;
                hull.pivotedSettings = null;
            }

            float scale = tiles == 1 ? 0.5f : 1f;
            if (tiles == 1)
            {
                settings.mass *= 0.3f;
                // The model, its colliders and its interaction box all shrink to one tile
                foreach (Transform child in prefab.transform.Cast<Transform>().ToList())
                    child.localScale = Vector3.Scale(child.localScale, new Vector3(0.5f, 0.5f, 1f));
            }
            // A thin box over the door's own area, so it never overlaps the walls around the hole
            var bounds = prefab.GetComponent<ShipPartBounds>();
            if (bounds != null)
            {
                bounds.center = Corner2 * scale;
                bounds.bounds = new Vector3(1.9f * scale, 1.9f * scale, 0.1f);
            }
        }

        /// <summary>
        /// The frame a floor door's content is placed with, in the ship: its rotation, and how far
        /// it moves from the game's placement position.
        /// </summary>
        public static void Frame(ushort id, Quaternion placementRotation, out Quaternion rotation, out Vector3 shift)
        {
            Vector3 up = placementRotation * Vector3.up;
            Vector3 forward = placementRotation * Vector3.forward;
            if (id == DockDown)
                forward = Vector3.down;
            else if (id == DockUp)
                forward = Vector3.up;
            rotation = Quaternion.LookRotation(forward, up);
            // Content centre (its corner tile grown by 2x2, or a shrunk 1x1) onto the tiles' centre
            bool single = id == Trapdoor1;
            Vector3 contentCenter = single ? Corner2 * 0.5f : Corner2;
            Vector3 tilesCenter = single ? Vector3.zero : new Vector3(0.5f, 0f, 0.5f);
            shift = tilesCenter - rotation * contentCenter;
        }

        public static bool TryFrame(StatefulPart part, out Quaternion rotation, out Vector3 position)
        {
            rotation = Quaternion.identity;
            position = Vector3.zero;
            if (part?.Settings == null || !IsFloorDoor(part.Settings.id) || part.Placement == null)
                return false;
            Frame(part.Settings.id, part.Placement.Rotation, out rotation, out Vector3 shift);
            position = part.Placement.LocalPosition + shift;
            return true;
        }
    }

    /// <summary>The new doors are in no research node, so the build menu would keep them locked.</summary>
    [HarmonyPatch(typeof(ResearchClientProxy), nameof(ResearchClientProxy.IsPartUnlocked))]
    internal static class HullDoorsUnlockPatch
    {
        private static void Postfix(PartSettings part, ref bool __result)
        {
            if (!__result && part != null && HullDoors.IsFloorDoor(part.id))
                __result = true;
        }
    }

    /// <summary>
    /// The part's rotation and position, read by the game to place the model, the interaction
    /// box and the colliders, and by the docking magnet: a floor door's recentred frame.
    /// </summary>
    [HarmonyPatch(typeof(StatefulPart))]
    internal static class FloorDoorFramePatch
    {
        [HarmonyPatch(nameof(StatefulPart.LocalRotation), MethodType.Getter)]
        [HarmonyPostfix]
        private static void Rotation(StatefulPart __instance, ref Quaternion __result)
        {
            if (HullDoors.TryFrame(__instance, out Quaternion rotation, out _))
                __result = rotation;
        }

        [HarmonyPatch(nameof(StatefulPart.LocalPosition), MethodType.Getter)]
        [HarmonyPostfix]
        private static void Position(StatefulPart __instance, ref Vector3 __result)
        {
            if (HullDoors.TryFrame(__instance, out _, out Vector3 position))
                __result = position;
        }
    }

    /// <summary>Colliders take the part's position but the placement's rotation: give them the door's.</summary>
    [HarmonyPatch(typeof(ShipStatefulColliders), nameof(ShipStatefulColliders.AddStatefulPart))]
    internal static class FloorDoorColliderPatch
    {
        private static void Postfix(ShipStatefulColliders __instance, StatefulPart part)
        {
            if (part?.Settings == null || !HullDoors.IsFloorDoor(part.Settings.id))
                return;
            if (Traverse.Create(__instance).Field("_dynamicColliders").GetValue() is IDictionary colliders
                && colliders.Contains(part.Id) && colliders[part.Id] is IShipPartDynamicCollider collider && collider.GameObject != null)
                collider.GameObject.transform.localRotation = part.LocalRotation;
        }
    }

    /// <summary>The build preview shows the door where and how it will really be.</summary>
    [HarmonyPatch(typeof(PartPreview), "UpdatePlacement")]
    internal static class FloorDoorPreviewPatch
    {
        private static void Postfix(PartPreview __instance)
        {
            var traverse = Traverse.Create(__instance);
            var settings = traverse.Field("_actualPreviewSettings").GetValue() as PartSettings;
            if (settings == null || !HullDoors.IsFloorDoor(settings.id))
                return;
            object snapped = traverse.Field("_snappedPlacement").GetValue();
            var ship = traverse.Field("_ship").GetValue() as IShipPartPlacements;
            var preview = traverse.Field("_objectPreview").GetValue() as GameObject;
            if (snapped == null || ship == null || preview == null)
                return;
            object placement = Traverse.Create(snapped).Property("HullPartPlacement").GetValue();
            if (!(placement is HullPartPlacement hull))
                return;
            HullDoors.Frame(settings.id, hull.Rotation, out Quaternion rotation, out Vector3 shift);
            preview.transform.rotation = ship.ToWorldRotation(rotation);
            Vector3 target = traverse.Field("_previewTargetPos").GetValue<Vector3>();
            traverse.Field("_previewTargetPos").SetValue(target + ship.ToWorldRotation(Quaternion.identity) * shift);
        }
    }

    /// <summary>
    /// Two docked ships are held where their doors meet. The game reads that from the doors'
    /// placements; for the floor docking doors it is redone from their real frame, once the joint
    /// tree is built (BuildShipGraph must not be patched: its graph is an out parameter that a
    /// patch reading __args sets back to null, which breaks every rotor and docking joint).
    /// </summary>
    [HarmonyPatch(typeof(ShipsJointsTreeTracker), "RecurseBuildChild")]
    internal static class HorizontalDockJointPatch
    {
        private static readonly Vector3 Center = new Vector3(0.5f, 0.5f, 0.25f);

        private static void Postfix(ShipsJointsTreeTracker __instance, TrackedShipJoint __result)
        {
            if (__result?.Children == null)
                return;
            IShipsProvider ships = null;
            foreach (TrackedShipJoint child in __result.Children)
            {
                if (!(child.JointConnection is DockedJointConnection joint))
                    continue;
                ships = ships ?? Traverse.Create(__instance).Field("_ships").GetValue() as IShipsProvider;
                if (ships == null)
                    return;
                // Without reversal the parent is ship A of the connection
                uint a = joint.IsDirectionReversed ? child.ShipId : __result.ShipId;
                uint b = joint.IsDirectionReversed ? __result.ShipId : child.ShipId;
                if (ships.TryGetShip(a, out IShipStateRead shipA) && ships.TryGetShip(b, out IShipStateRead shipB))
                    Fix(joint, shipA, shipB);
            }
        }

        private static void Fix(DockedJointConnection joint, IShipStateRead shipA, IShipStateRead shipB)
        {
            foreach (StatefulPart doorA in shipA.GetAllStatefulParts<DockingDoorState>())
            {
                var state = (DockingDoorState)doorA.State;
                if (!state.IsDocked || state.DockedConnectedPart.ShipId != shipB.Id)
                    continue;
                if (!shipB.TryGetStatefulPart(state.DockedConnectedPart.PartId, out StatefulPart doorB))
                    continue;
                // The joint the game built from this pair, with the placements as they are stored
                Vector3 builtA = doorA.Placement.LocalPosition + doorA.Placement.Rotation * Center;
                if ((builtA - joint.PositionA).sqrMagnitude > 0.0001f)
                    continue;
                if (!HullDoors.IsFloorDoor(doorA.Settings.id) && !HullDoors.IsFloorDoor(doorB.Settings.id))
                    return;
                joint.PositionA = doorA.LocalPosition + doorA.LocalRotation * Center;
                joint.RotationA = doorA.LocalRotation * Quaternion.LookRotation(Vector3.back, Vector3.up);
                joint.PositionB = doorB.LocalPosition + doorB.LocalRotation * Center;
                joint.RotationB = doorB.LocalRotation;
                return;
            }
        }
    }
}
