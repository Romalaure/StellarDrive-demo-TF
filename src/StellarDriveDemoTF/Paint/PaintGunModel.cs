using System.Collections.Generic;
using HarmonyLib;
using Players.Visuals;
using StellarDriveDemoTF.Common;
using Tools.Interface.Model;
using Tools.PaintTool;
using UnityEngine;
using UnityEngine.Rendering;

namespace StellarDriveDemoTF.Paint
{
    /// <summary>
    /// Replaces the demo's placeholder paint tool (a box, a handle and a can) with a gravity-feed
    /// spray gun whose paint cup and nozzle tip show the selected color.
    /// Built on every tool instance: the local first-person tool and the copies other players see.
    /// </summary>
    internal static class PaintGunModel
    {
        private const string ModelName = "TF_PaintGun";
        private const string OldMeshName = "PaintTool";
        private const string OldPreviewName = "ColorPreview";

        internal enum Part { Body, Dark, Accent, Chrome, Paint, Glow }

        internal static readonly Color DefaultPaint = new Color(0.95f, 0.45f, 0.1f);

        private static Dictionary<Part, Mesh> _meshes;
        private static readonly Dictionary<Part, Material> Materials = new Dictionary<Part, Material>();

        /// <summary>Swaps the model under a tool's "Visuals" transform. Safe to call repeatedly.</summary>
        public static void Install(Transform visuals, bool firstPerson)
        {
            if (visuals == null || visuals.Find(ModelName) != null)
                return;
            Transform oldMesh = visuals.Find(OldMeshName);
            var oldRenderer = oldMesh != null ? oldMesh.GetComponent<MeshRenderer>() : null;
            if (oldRenderer == null)
                return;

            EnsureAssets(oldRenderer.sharedMaterial);

            var root = new GameObject(ModelName);
            root.layer = oldMesh.gameObject.layer;
            root.transform.SetParent(visuals, false);
            root.transform.localPosition = oldMesh.localPosition;
            root.transform.localRotation = oldMesh.localRotation;
            root.transform.localScale = oldMesh.localScale;

            foreach (KeyValuePair<Part, Mesh> entry in _meshes)
            {
                var child = new GameObject(ModelName + "_" + entry.Key);
                child.layer = root.layer;
                child.transform.SetParent(root.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = entry.Value;
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = Materials[entry.Key];
                renderer.shadowCastingMode = firstPerson ? ShadowCastingMode.Off : oldRenderer.shadowCastingMode;
                renderer.receiveShadows = oldRenderer.receiveShadows;
            }

            oldRenderer.enabled = false;
            foreach (Transform child in visuals.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == OldPreviewName && child.TryGetComponent(out MeshRenderer preview))
                    preview.enabled = false;
            }

            root.AddComponent<PaintGunColor>();
        }

        private static void EnsureAssets(Material template)
        {
            if (_meshes == null)
                _meshes = BuildMeshes();

            if (Materials.TryGetValue(Part.Body, out Material existing) && existing != null)
                return;
            Materials.Clear();
            Materials[Part.Body] = MakeMaterial(template, Part.Body, new Color(0.72f, 0.75f, 0.79f), 0.55f, 0.55f);
            Materials[Part.Dark] = MakeMaterial(template, Part.Dark, new Color(0.11f, 0.115f, 0.125f), 0.1f, 0.3f);
            Materials[Part.Accent] = MakeMaterial(template, Part.Accent, new Color(0.96f, 0.47f, 0.08f), 0.2f, 0.5f);
            Materials[Part.Chrome] = MakeMaterial(template, Part.Chrome, new Color(0.86f, 0.87f, 0.9f), 1f, 0.85f);
            Materials[Part.Paint] = MakeMaterial(template, Part.Paint, DefaultPaint, 0f, 0.8f);
            Materials[Part.Glow] = MakeMaterial(template, Part.Glow, DefaultPaint, 0f, 0.9f);
            Materials[Part.Glow].EnableKeyword("_EMISSION");
            Materials[Part.Glow].globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            Materials[Part.Glow].SetColor("_EmissionColor", DefaultPaint * 1.5f);
        }

        private static Material MakeMaterial(Material template, Part part, Color color, float metallic, float smoothness)
        {
            Material material = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = ModelName + "_" + part;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", null);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", null);
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        private static Dictionary<Part, Mesh> BuildMeshes()
        {
            var meshes = new Dictionary<Part, Mesh>();
            foreach (KeyValuePair<Part, MeshBuilder> entry in BuildShapes())
                meshes[entry.Key] = entry.Value.Build(ModelName + "_" + entry.Key);
            return meshes;
        }

        // Unity units are meters; +z is the spray direction, +y is up, the handle sits around z = 0
        internal static Dictionary<Part, MeshBuilder> BuildShapes()
        {
            var b = new Dictionary<Part, MeshBuilder>();
            foreach (Part part in System.Enum.GetValues(typeof(Part)))
                b[part] = new MeshBuilder();

            const float bodyY = 0.118f;
            const float cupZ = 0.035f;

            // Body: round barrel with a flat underside that meets the handle
            b[Part.Body].Cylinder(new Vector3(0f, bodyY, -0.07f), new Vector3(0f, bodyY, 0.085f), 0.03f, 0.03f, 20);
            b[Part.Body].Box(new Vector3(0f, 0.096f, 0.005f), new Vector3(0.044f, 0.03f, 0.13f));
            // Accent stripe along the barrel
            b[Part.Accent].Cylinder(new Vector3(0f, bodyY, 0.045f), new Vector3(0f, bodyY, 0.06f), 0.0315f, 0.0315f, 20);

            // Front: chrome collar, orange air cap with its two horns, fluid tip and glowing nozzle
            b[Part.Chrome].Cylinder(new Vector3(0f, bodyY, 0.085f), new Vector3(0f, bodyY, 0.1f), 0.031f, 0.027f, 20);
            b[Part.Accent].Cylinder(new Vector3(0f, bodyY, 0.1f), new Vector3(0f, bodyY, 0.124f), 0.027f, 0.019f, 20);
            b[Part.Accent].Box(new Vector3(0.022f, bodyY, 0.128f), new Vector3(0.009f, 0.02f, 0.024f));
            b[Part.Accent].Box(new Vector3(-0.022f, bodyY, 0.128f), new Vector3(0.009f, 0.02f, 0.024f));
            b[Part.Chrome].Cylinder(new Vector3(0f, bodyY, 0.124f), new Vector3(0f, bodyY, 0.138f), 0.009f, 0.006f, 12);
            b[Part.Glow].Cylinder(new Vector3(0f, bodyY, 0.138f), new Vector3(0f, bodyY, 0.143f), 0.0045f, 0.0035f, 12);

            // Rear: fan width knob (top) and fluid flow knob (bottom)
            b[Part.Dark].Cylinder(new Vector3(0f, 0.13f, -0.07f), new Vector3(0f, 0.13f, -0.09f), 0.011f, 0.011f, 10);
            b[Part.Chrome].Cylinder(new Vector3(0f, 0.13f, -0.09f), new Vector3(0f, 0.13f, -0.094f), 0.008f, 0.008f, 10);
            b[Part.Dark].Cylinder(new Vector3(0f, 0.1f, -0.07f), new Vector3(0f, 0.1f, -0.086f), 0.008f, 0.008f, 10);

            // Side pressure gauge, its face shows the paint color
            b[Part.Chrome].Cylinder(new Vector3(-0.026f, bodyY, 0.02f), new Vector3(-0.038f, bodyY, 0.02f), 0.014f, 0.014f, 16);
            b[Part.Glow].Cylinder(new Vector3(-0.038f, bodyY, 0.02f), new Vector3(-0.0395f, bodyY, 0.02f), 0.011f, 0.011f, 16);

            // Gravity cup on top: chrome neck, collar, paint-colored cup, dark lid and vent cap
            b[Part.Chrome].Cylinder(new Vector3(0f, 0.142f, cupZ), new Vector3(0f, 0.158f, cupZ), 0.009f, 0.009f, 12);
            b[Part.Body].Cylinder(new Vector3(0f, 0.158f, cupZ), new Vector3(0f, 0.166f, cupZ), 0.02f, 0.029f, 20);
            b[Part.Paint].Cylinder(new Vector3(0f, 0.166f, cupZ), new Vector3(0f, 0.212f, cupZ), 0.029f, 0.033f, 20);
            b[Part.Dark].Cylinder(new Vector3(0f, 0.212f, cupZ), new Vector3(0f, 0.22f, cupZ), 0.035f, 0.035f, 20);
            b[Part.Accent].Cylinder(new Vector3(0f, 0.22f, cupZ), new Vector3(0f, 0.228f, cupZ), 0.01f, 0.008f, 12);

            // Handle, tilted back, with a wider butt
            b[Part.Dark].Block(new[]
            {
                new Vector3(-0.021f, -0.085f, -0.072f), new Vector3(0.021f, -0.085f, -0.072f),
                new Vector3(-0.019f, 0.085f, -0.035f), new Vector3(0.019f, 0.085f, -0.035f),
                new Vector3(-0.021f, -0.085f, -0.022f), new Vector3(0.021f, -0.085f, -0.022f),
                new Vector3(-0.019f, 0.085f, 0.022f), new Vector3(0.019f, 0.085f, 0.022f)
            });
            b[Part.Body].Block(new[]
            {
                new Vector3(-0.024f, -0.097f, -0.078f), new Vector3(0.024f, -0.097f, -0.078f),
                new Vector3(-0.024f, -0.085f, -0.076f), new Vector3(0.024f, -0.085f, -0.076f),
                new Vector3(-0.024f, -0.097f, -0.018f), new Vector3(0.024f, -0.097f, -0.018f),
                new Vector3(-0.024f, -0.085f, -0.018f), new Vector3(0.024f, -0.085f, -0.018f)
            });

            // Air inlet under the handle with an orange hose fitting
            b[Part.Chrome].Cylinder(new Vector3(0f, -0.097f, -0.048f), new Vector3(0f, -0.122f, -0.048f), 0.0075f, 0.0075f, 12);
            b[Part.Accent].Cylinder(new Vector3(0f, -0.104f, -0.048f), new Vector3(0f, -0.112f, -0.048f), 0.0105f, 0.0105f, 12);

            // Trigger: two chrome segments curving down in front of the handle
            b[Part.Chrome].Block(new[]
            {
                new Vector3(-0.011f, 0.045f, 0.03f), new Vector3(0.011f, 0.045f, 0.03f),
                new Vector3(-0.011f, 0.083f, 0.028f), new Vector3(0.011f, 0.083f, 0.028f),
                new Vector3(-0.011f, 0.045f, 0.04f), new Vector3(0.011f, 0.045f, 0.04f),
                new Vector3(-0.011f, 0.083f, 0.038f), new Vector3(0.011f, 0.083f, 0.038f)
            });
            b[Part.Chrome].Block(new[]
            {
                new Vector3(-0.011f, 0.002f, 0.018f), new Vector3(0.011f, 0.002f, 0.018f),
                new Vector3(-0.011f, 0.045f, 0.03f), new Vector3(0.011f, 0.045f, 0.03f),
                new Vector3(-0.011f, 0.002f, 0.027f), new Vector3(0.011f, 0.002f, 0.027f),
                new Vector3(-0.011f, 0.045f, 0.04f), new Vector3(0.011f, 0.045f, 0.04f)
            });

            return b;
        }
    }

    /// <summary>Keeps the cup and nozzle of the local player's spray gun in the selected color.</summary>
    internal sealed class PaintGunColor : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private Renderer[] _paintRenderers;
        private Renderer[] _glowRenderers;
        private PaintTool _tool;
        private Color _shown = new Color(-1f, 0f, 0f);

        private void Awake()
        {
            _paintRenderers = FindRenderers(PaintGunModel.Part.Paint);
            _glowRenderers = FindRenderers(PaintGunModel.Part.Glow);
            _tool = GetComponentInParent<PaintTool>();
        }

        private void Update()
        {
            // Only the local tool has an active PaintTool; copies seen on other players keep the default
            Color color = PaintGunModel.DefaultPaint;
            if (_tool != null && _tool.enabled)
            {
                var selection = GameServices.PaintSelection;
                if (selection != null)
                    color = selection.SelectedPaintColor;
            }
            if (color == _shown)
                return;
            _shown = color;

            foreach (Renderer renderer in _paintRenderers)
            {
                renderer.GetPropertyBlock(_block);
                _block.SetColor(BaseColor, color);
                renderer.SetPropertyBlock(_block);
            }
            foreach (Renderer renderer in _glowRenderers)
            {
                renderer.GetPropertyBlock(_block);
                _block.SetColor(BaseColor, color);
                _block.SetColor(EmissionColor, color * 1.5f);
                renderer.SetPropertyBlock(_block);
            }
        }

        private Renderer[] FindRenderers(PaintGunModel.Part part)
        {
            Transform child = transform.Find(gameObject.name + "_" + part);
            return child != null ? child.GetComponents<Renderer>() : new Renderer[0];
        }
    }

    [HarmonyPatch(typeof(PaintTool), nameof(PaintTool.EnableTool))]
    internal static class PaintToolEnablePatch
    {
        private static void Postfix(PaintTool __instance)
        {
            ToolVisuals visuals = __instance.GetComponentInChildren<ToolVisuals>(true);
            if (visuals != null)
                PaintGunModel.Install(visuals.transform, firstPerson: true);
        }
    }

    [HarmonyPatch(typeof(OtherPlayerToolVisual), "OnItemSelectionChange")]
    internal static class OtherPlayerToolVisualPatch
    {
        private static readonly AccessTools.FieldRef<OtherPlayerToolVisual, ToolVisuals> CurrentTool =
            AccessTools.FieldRefAccess<OtherPlayerToolVisual, ToolVisuals>("_currentTool");

        private static void Postfix(OtherPlayerToolVisual __instance)
        {
            ToolVisuals visuals = CurrentTool(__instance);
            if (visuals != null)
                PaintGunModel.Install(visuals.transform, firstPerson: false);
        }
    }
}
