using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Research;
using SDModKit.Game;
using Ships;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.Common;
using Ships.Interface.Model.Parts.State;
using Ships.Interface.Model.Parts.StateTypes;
using Ships.Interface.Settings;
using Ships.Parts.Gyroscope;
using StellarDriveDemoTF.Common;
using StellarDriveDemoTF.Lights;
using UnityEngine;
using UnityEngine.Rendering;

namespace StellarDriveDemoTF.Chairs
{
    /// <summary>
    /// Chairs are clones of the game's basic pilot seat, the only part a player can sit in: the
    /// seat's model is hidden (its flight sticks keep their transforms so its animators still work)
    /// and a new model is built in its place. Sit-only chairs lose their signal plugs and are
    /// patched on the server so they never steer the ship; the copilot seat keeps everything.
    /// Only call Register when SDModKit is loaded.
    /// </summary>
    internal static class ChairParts
    {
        private const string Donor = "BasicPilotSeat";

        public static void Register()
        {
            foreach (ChairSpec chair in ChairCatalog.All)
            {
                ChairSpec spec = chair;
                CustomParts.Register(new CustomPartDefinition
                {
                    Id = spec.Id,
                    Name = "TF_" + spec.Id,
                    Donor = Donor,
                    BuildTab = TFTab.Name,
                    BuildRow = TFTab.ChairsRow,
                    Configure = (settings, prefab) => Configure(spec, settings, prefab)
                });
            }
            TFMod.Log.Msg($"registered {ChairCatalog.All.Length} chairs");
        }

        private static void Configure(ChairSpec spec, PartSettings settings, GameObject prefab)
        {
            settings.fullLabel = spec.Label;
            bool sitOnly = ChairCatalog.IsSitOnly(spec.Id);
            settings.description = sitOnly && spec.CanPilot
                ? spec.Description.Split(new[] { " C'est un vrai poste" }, System.StringSplitOptions.None)[0] + " Décoratif : il ne pilote pas (réglage CopilotCanFly pour l'activer)."
                : spec.Description;
            settings.localizedDescription = null;
            // Chairs are furniture: next to no weight, so they never change how the ship handles
            settings.mass = sitOnly ? 0.01f : spec.Mass;
            LampParts.SetCost(settings, spec.Cost);

            var bounds = prefab.GetComponent<ShipPartBounds>();
            if (bounds != null)
            {
                bounds.center = spec.BoundsCenter;
                bounds.bounds = spec.BoundsSize;
            }
            Transform interactions = prefab.transform.Find("Interactions");
            if (interactions != null)
            {
                foreach (BoxCollider box in interactions.GetComponentsInChildren<BoxCollider>(true))
                {
                    box.transform.localPosition = spec.BoundsCenter;
                    box.transform.localRotation = Quaternion.identity;
                    box.center = Vector3.zero;
                    box.size = spec.BoundsSize;
                }
            }

            if (sitOnly)
            {
                foreach (SocketObject socket in prefab.GetComponentsInChildren<SocketObject>(true))
                {
                    if (socket.visualObject != null)
                        Object.DestroyImmediate(socket.visualObject);
                    if (socket != null && socket.interactive != null)
                        Object.DestroyImmediate(socket.interactive);
                    if (socket != null)
                        Object.DestroyImmediate(socket.gameObject);
                }
            }

            Transform visuals = prefab.transform.Find("Visuals");
            Transform seat = visuals.Find("Seat");
            if (seat != null)
            {
                // Hide the pilot seat and its sticks but keep their transforms for the seat's animators
                foreach (Renderer renderer in seat.GetComponentsInChildren<Renderer>(true))
                    Object.DestroyImmediate(renderer);
                foreach (MeshFilter filter in seat.GetComponentsInChildren<MeshFilter>(true))
                    Object.DestroyImmediate(filter);
            }
            BuildModel(spec, visuals);
        }

        private static void BuildModel(ChairSpec spec, Transform visuals)
        {
            int layer = visuals.gameObject.layer;
            foreach (KeyValuePair<ChairGroup, MeshBuilder> entry in spec.BuildShapes())
            {
                if (!spec.Materials.TryGetValue(entry.Key, out ChairMaterial look))
                    continue;
                // Glowing strips keep their color; everything else takes the paint
                string name = look.Glows ? "TF_NoPaint_ChairGlow" : "TF_Chair_" + entry.Key;
                var child = new GameObject(name) { layer = layer };
                child.transform.SetParent(visuals, false);
                child.AddComponent<MeshFilter>().sharedMesh = entry.Value.Build("TF_Chair" + spec.Id + "_" + entry.Key);
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = LitMaterials.Get($"TF_Chair{spec.Id}_{entry.Key}", look.Color, look.Metallic, look.Smoothness,
                    look.Glows ? look.Color * 2.5f : (Color?)null);
                renderer.shadowCastingMode = look.Glows ? ShadowCastingMode.Off : ShadowCastingMode.On;
            }
        }
    }

    /// <summary>Sit-only chairs ignore the flight controls of whoever sits in them.</summary>
    [HarmonyPatch(typeof(TrackedShipServer), nameof(TrackedShipServer.SetFlightInputs))]
    internal static class ChairFlightInputsPatch
    {
        private static bool Prefix(TrackedShipServer __instance, ushort controlPart) =>
            !(__instance.TryGetStatefulPart(controlPart, out StatefulPart part) && part.Settings != null && ChairCatalog.IsSitOnly(part.Settings.id));
    }

    /// <summary>A seated passenger must not act as the ship's gyroscope (it would damp the pilot's turns).</summary>
    [HarmonyPatch(typeof(GyroscopesUpdater), "CalculateGyroStrength")]
    internal static class ChairGyroPatch
    {
        private static bool Prefix(StatefulPart part, ref Vector3 __result)
        {
            if (part?.Settings == null || !ChairCatalog.IsSitOnly(part.Settings.id))
                return true;
            __result = Vector3.zero;
            return false;
        }
    }

    /// <summary>The ship's flight orientation follows a seat in use; sit-only chairs never count.</summary>
    [HarmonyPatch(typeof(TrackedShipServer), "GetShipPilotSeatOrientation")]
    internal static class ChairFlightOrientationPatch
    {
        private static bool Prefix(TrackedShipServer ship, ref Quaternion? __result)
        {
            if (ship == null)
                return true;
            __result = ship.GetAllStatefulParts<PilotSeatState>()
                .Concat(ship.GetAllStatefulParts<PilotSeatAdvState>())
                .Where(p => ((IDirectlyInteractivePart)p.State).IsInUse && (p.Settings == null || !ChairCatalog.IsSitOnly(p.Settings.id)))
                .FirstOrDefault()?.LocalRotation;
            return false;
        }
    }

    /// <summary>Chairs are in no research node, so the build menu would keep them locked forever.</summary>
    [HarmonyPatch(typeof(ResearchClientProxy), nameof(ResearchClientProxy.IsPartUnlocked))]
    internal static class ChairUnlockPatch
    {
        private static void Postfix(PartSettings part, ref bool __result)
        {
            if (!__result && part != null && ChairCatalog.IsChair(part.id))
                __result = true;
        }
    }
}
