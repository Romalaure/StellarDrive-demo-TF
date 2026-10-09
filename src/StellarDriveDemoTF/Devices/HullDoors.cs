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
using Ships.Interface.Settings;
using Ships.Joints;
using StellarDriveDemoTF.Common;
using UnityEngine;

namespace StellarDriveDemoTF.Devices
{
    /// <summary>
    /// Doors laid in the floor: trapdoors (the game's door, 2x2 or shrunk to 1x1) and horizontal
    /// docking doors (the game's docking door, docking downward or upward). They are the game's
    /// own parts, cloned, set to snap like floor tiles, with their model, colliders and
    /// interaction box turned 90 degrees under the part: opening, closing, saving and syncing
    /// all stay the game's. For the docking doors, the magnet and the docking joint are turned
    /// the same way, so two ships dock one above the other.
    /// </summary>
    internal static class HullDoors
    {
        public const ushort Trapdoor2 = 7181;
        public const ushort Trapdoor1 = 7182;
        public const ushort DockDown = 7183;
        public const ushort DockUp = 7184;

        /// <summary>How the door's content sits in the part: rotation and offset, for the magnet and joint.</summary>
        private static readonly Dictionary<ushort, (Quaternion Rotation, Vector3 Offset)> Tilts = new Dictionary<ushort, (Quaternion, Vector3)>
        {
            [Trapdoor2] = (Quaternion.Euler(90f, 0f, 0f), Vector3.zero),
            [Trapdoor1] = (Quaternion.Euler(90f, 0f, 0f), new Vector3(-0.25f, 0f, -0.25f)),
            // Outward side (+z of the wall door) down
            [DockDown] = (Quaternion.Euler(90f, 0f, 0f), Vector3.zero),
            // Outward side up; the turn moves the door to -z, shifted back over its tiles
            [DockUp] = (Quaternion.Euler(-90f, 0f, 0f), new Vector3(0f, 0f, 1f))
        };

        public static bool IsTilted(ushort id) => Tilts.ContainsKey(id);

        public static bool IsDock(ushort id) => id == DockDown || id == DockUp;

        public static void Register()
        {
            Add(Trapdoor2, "Door", "Trappe 2×2",
                "Porte posée dans le sol, sur 2×2 cases : retire 4 dalles de sol et pose la trappe à leur place. S'ouvre et se ferme comme une porte.",
                2, 1f);
            Add(Trapdoor1, "Door", "Trappe 1×1",
                "Petite porte posée dans le sol, sur une seule case : retire une dalle de sol et pose la trappe à sa place. S'ouvre et se ferme comme une porte.",
                1, 0.5f);
            Add(DockDown, "DockingDoor", "Porte d'amarrage (sol, vers le bas)",
                "Porte d'amarrage posée dans le sol, sur 2×2 cases, qui s'amarre vers le BAS : sous le vaisseau, elle accroche une porte d'amarrage tournée vers le haut. Mêmes fonctions que la porte d'amarrage du jeu.",
                2, 1f);
            Add(DockUp, "DockingDoor", "Porte d'amarrage (sol, vers le haut)",
                "Porte d'amarrage posée dans le sol, sur 2×2 cases, qui s'amarre vers le HAUT : sur le toit d'un vaisseau, elle accroche une porte d'amarrage tournée vers le bas. Mêmes fonctions que la porte d'amarrage du jeu.",
                2, 1f);
            TFMod.Log.Msg("registered trapdoors and horizontal docking doors");
        }

        private static void Add(ushort id, string donor, string label, string description, int tiles, float scale)
        {
            CustomParts.Register(new CustomPartDefinition
            {
                Id = id,
                Name = "TF_" + id,
                Donor = donor,
                BuildTab = TFTab.Name,
                BuildRow = TFTab.DoorsRow,
                Configure = (settings, prefab) => Configure(id, settings, prefab, label, description, tiles, scale)
            });
        }

        private static void Configure(ushort id, PartSettings settings, GameObject prefab, string label, string description, int tiles, float scale)
        {
            settings.fullLabel = label;
            settings.description = description;
            settings.localizedDescription = null;
            settings.snappingStyle = SnappingStyle.Floor;
            settings.size = new Vector3(tiles, 1f, tiles);
            if (settings is HullPartSettings hull)
            {
                hull.defaultForward = PartOrientation.Forward;
                hull.defaultUp = PartOrientation.Up;
                hull.pivotedSettings = null;
            }
            if (tiles == 1)
                settings.mass *= 0.3f;

            (Quaternion rotation, Vector3 offset) = Tilts[id];
            var contentScale = new Vector3(scale, scale, 1f);
            // Each top-level container (model, interaction, colliders) turns and shrinks around the part origin
            foreach (Transform child in prefab.transform.Cast<Transform>().ToList())
            {
                child.localPosition = offset + rotation * Vector3.Scale(contentScale, child.localPosition);
                child.localRotation = rotation * child.localRotation;
                child.localScale = Vector3.Scale(child.localScale, contentScale);
            }

            var bounds = prefab.GetComponent<ShipPartBounds>();
            if (bounds != null)
            {
                Vector3 center = offset + rotation * Vector3.Scale(contentScale, bounds.center);
                Vector3 size = rotation * Vector3.Scale(contentScale, bounds.bounds);
                bounds.center = center;
                bounds.bounds = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            }
        }

        /// <summary>The door's own frame in the ship: rotation and position of what the wall door would be.</summary>
        public static bool TryTilt(StatefulPart part, out Quaternion rotation, out Vector3 offset)
        {
            rotation = Quaternion.identity;
            offset = Vector3.zero;
            if (part?.Settings == null || !Tilts.TryGetValue(part.Settings.id, out var tilt))
                return false;
            rotation = tilt.Rotation;
            offset = tilt.Offset;
            return true;
        }
    }

    /// <summary>The new doors are in no research node, so the build menu would keep them locked.</summary>
    [HarmonyPatch(typeof(ResearchClientProxy), nameof(ResearchClientProxy.IsPartUnlocked))]
    internal static class HullDoorsUnlockPatch
    {
        private static void Postfix(PartSettings part, ref bool __result)
        {
            if (!__result && part != null && HullDoors.IsTilted(part.id))
                __result = true;
        }
    }

    /// <summary>
    /// A horizontal docking door's magnet (attraction points, alignment) points up or down like
    /// its model. The game's magnet data reads the part's rotation and position from these two
    /// properties, so they are turned for the horizontal docking doors only. (Patching the
    /// magnet struct itself crashed the game: Mono and Harmony disagree on struct methods that
    /// return structs.)
    /// </summary>
    [HarmonyPatch(typeof(StatefulPart))]
    internal static class HorizontalDockFramePatch
    {
        [HarmonyPatch(nameof(StatefulPart.LocalRotation), MethodType.Getter)]
        [HarmonyPostfix]
        private static void Rotation(StatefulPart __instance, ref Quaternion __result)
        {
            if (__instance.Settings != null && HullDoors.IsDock(__instance.Settings.id) && HullDoors.TryTilt(__instance, out Quaternion tilt, out _))
                __result *= tilt;
        }

        [HarmonyPatch(nameof(StatefulPart.LocalPosition), MethodType.Getter)]
        [HarmonyPostfix]
        private static void Position(StatefulPart __instance, ref Vector3 __result)
        {
            if (__instance.Settings != null && HullDoors.IsDock(__instance.Settings.id) && HullDoors.TryTilt(__instance, out _, out Vector3 offset)
                && offset != Vector3.zero)
                __result += __instance.Placement.Rotation * offset;
        }
    }

    /// <summary>
    /// Two docked ships are held where their doors meet. The game computes that from the doors'
    /// placements as wall doors; for a horizontal docking door it is recomputed with its turn.
    /// </summary>
    [HarmonyPatch(typeof(ShipsJointsTreeTracker), "BuildShipGraph")]
    internal static class HorizontalDockJointPatch
    {
        private static readonly Vector3 Center = new Vector3(0.5f, 0.5f, 0.25f);

        private static void Postfix(object[] __args)
        {
            object graph = __args.Length > 1 ? __args[1] : null;
            if (graph == null)
                return;
            var nodes = Traverse.Create(graph).Property("Nodes").GetValue() as IDictionary;
            if (nodes == null)
                return;
            var fixedConnections = new HashSet<DockedJointConnection>();
            foreach (object node in nodes.Values)
            {
                if (!(Traverse.Create(node).Property("Connections").GetValue() is IList connections))
                    continue;
                foreach (object connection in connections)
                {
                    var traverse = Traverse.Create(connection);
                    if (!(traverse.Property("Connection").GetValue() is DockedJointConnection joint) || !fixedConnections.Add(joint))
                        continue;
                    var shipA = Traverse.Create(traverse.Property("ShipA").GetValue()).Property("ShipRef").GetValue() as IShipStateRead;
                    var shipB = Traverse.Create(traverse.Property("ShipB").GetValue()).Property("ShipRef").GetValue() as IShipStateRead;
                    if (shipA != null && shipB != null)
                        Fix(joint, shipA, shipB);
                }
            }
        }

        // Finds the pair of docked doors this joint was built from and redoes it if one is horizontal
        private static void Fix(DockedJointConnection joint, IShipStateRead shipA, IShipStateRead shipB)
        {
            foreach (StatefulPart doorA in shipA.GetAllStatefulParts<DockingDoorState>())
            {
                var state = (DockingDoorState)doorA.State;
                if (!state.IsDocked || state.DockedConnectedPart.ShipId != shipB.Id)
                    continue;
                if (!shipB.TryGetStatefulPart(state.DockedConnectedPart.PartId, out StatefulPart doorB))
                    continue;
                Vector3 wallPositionA = doorA.Placement.LocalPosition + doorA.Placement.Rotation * Center;
                if ((wallPositionA - joint.PositionA).sqrMagnitude > 0.0001f)
                    continue;
                bool tiltedA = HullDoors.TryTilt(doorA, out Quaternion tiltA, out Vector3 offsetA);
                bool tiltedB = HullDoors.TryTilt(doorB, out Quaternion tiltB, out Vector3 offsetB);
                if (!tiltedA && !tiltedB)
                    return;
                Quaternion rotationA = doorA.Placement.Rotation * tiltA;
                Quaternion rotationB = doorB.Placement.Rotation * tiltB;
                joint.PositionA = doorA.Placement.LocalPosition + doorA.Placement.Rotation * (offsetA + tiltA * Center);
                joint.RotationA = rotationA * Quaternion.LookRotation(Vector3.back, Vector3.up);
                joint.PositionB = doorB.Placement.LocalPosition + doorB.Placement.Rotation * (offsetB + tiltB * Center);
                joint.RotationB = rotationB;
                return;
            }
        }
    }
}
