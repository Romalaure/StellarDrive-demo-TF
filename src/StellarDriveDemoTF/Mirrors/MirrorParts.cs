using System.Collections.Generic;
using Ships.Interface.Settings;
using StellarDriveDemoTF.Common;
using StellarDriveDemoTF.Lights;
using UnityEngine;
using UnityEngine.Rendering;

namespace StellarDriveDemoTF.Mirrors
{
    /// <summary>Registers the mirrors as SDModKit parts, next to the lamps. Only call when SDModKit is loaded.</summary>
    internal static class MirrorParts
    {
        public static void Register()
        {
            foreach (MirrorSpec mirror in MirrorCatalog.All)
            {
                MirrorSpec spec = mirror;
                LampParts.RegisterDonorPart(spec.Id, LampParts.BuildTab, (settings, prefab) => Configure(spec, settings, prefab));
            }
            TFMod.Log.Msg($"registered {MirrorCatalog.All.Length} mirrors");
        }

        private static void Configure(MirrorSpec spec, PartSettings settings, GameObject prefab)
        {
            Transform visuals = LampParts.PrepareDonor(settings, prefab, spec.Label, spec.Description, spec.Mass, spec.Cost,
                spec.BoundsCenter, spec.BoundsSize, spec.SocketPosition);
            int layer = visuals.gameObject.layer;

            Material frame = LitMaterials.Get("TF_MirrorFrame", new Color(0.16f, 0.17f, 0.19f), 0.5f, 0.5f);
            Material trim = LitMaterials.Get("TF_MirrorTrim", new Color(0.8f, 0.81f, 0.84f), 1f, 0.85f);
            foreach (KeyValuePair<MirrorGroup, MeshBuilder> entry in spec.BuildShapes())
                AddRenderer(visuals, "TF_NoPaint_" + entry.Key, entry.Value.Build("TF_Mirror" + spec.Id + "_" + entry.Key),
                    entry.Key == MirrorGroup.Frame ? frame : trim, layer, ShadowCastingMode.On);

            // Shows the mirror camera's image through emission, so it is not darkened by the room's lighting.
            // "glass" in the name keeps the paint tool off it.
            Material glass = LitMaterials.Get("TF_MirrorGlass", Color.black, 0f, 0.96f, Color.white);
            AddRenderer(visuals, MirrorVisuals.GlassName, MirrorCatalog.BuildGlass(spec), glass, layer, ShadowCastingMode.Off);

            visuals.gameObject.AddComponent<MirrorVisuals>();
        }

        private static void AddRenderer(Transform parent, string name, Mesh mesh, Material material, int layer, ShadowCastingMode shadows)
        {
            var child = new GameObject(name) { layer = layer };
            child.transform.SetParent(parent, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows;
        }
    }
}
