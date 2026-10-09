using Ships.Parts.Common.Model.Reactions;
using StellarDriveDemoTF.Common;
using UnityEngine;

namespace StellarDriveDemoTF.Paint
{
    /// <summary>
    /// Applies paint to a part's visual through property blocks, so shared materials stay untouched.
    /// Ship parts use the game's Custom/PlanetObject shader (_Color tint, _Roughness, _Metalness,
    /// _EmissionColor); URP Lit names are set too for parts added by other mods.
    /// </summary>
    internal static class PartTint
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int RoughnessId = Shader.PropertyToID("_Roughness");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int MetalnessId = Shader.PropertyToID("_Metalness");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        // Glow 100% gives this much HDR emission
        private const float MaxGlow = 4f;

        // Materials whose look depends on their own color: glass, screens, holograms, fluids
        private static readonly string[] SkippedMaterialWords = { "glass", "screen", "display", "holo", "window", "fluid", "liquid", "laser", "text", "font", "sdf" };
        private static readonly string[] SkippedShaderWords = { "hologram", "screen", "textmesh", "particles", "laser", "transparent", "window", "display", "warp", "skybox" };

        private static readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();

        /// <summary>Re-applies the stored paint (or removes it) of a part, if its visual is loaded.</summary>
        public static void Refresh(PartKey key)
        {
            var ships = GameServices.ShipsClient;
            if (ships == null || !ships.TryGetTrackedShip(key.ShipId, out var ship))
                return;
            if (!ship.TryGetFloatingPartVisual(key.PartId, out IShipPartVisual visual) || visual == null)
                return;
            Apply(visual.GameObject, key);
        }

        public static void Apply(GameObject root, PartKey key)
        {
            if (root == null)
                return;
            if (PaintNet.Client.TryGet(key, out PaintData paint))
                Apply(root, paint);
            else
                Clear(root);
        }

        private static void Apply(GameObject root, PaintData paint)
        {
            Color color = paint.Color;
            PaintFinish finish = paint.Finish;
            foreach (Renderer renderer in Paintable(root))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (!IsPaintable(material))
                        continue;

                    renderer.GetPropertyBlock(Block, i);
                    Block.SetColor(ColorId, color);
                    Block.SetColor(BaseColorId, color);
                    if (finish.Enabled)
                    {
                        Block.SetFloat(RoughnessId, 1f - finish.GlossValue);
                        Block.SetFloat(SmoothnessId, finish.GlossValue);
                        Block.SetFloat(MetalnessId, finish.MetalValue);
                        Block.SetFloat(MetallicId, finish.MetalValue);
                        Block.SetColor(EmissionColorId, color * (finish.GlowValue * MaxGlow));
                    }
                    else
                    {
                        // Back to the material's own values
                        Block.Clear();
                        Block.SetColor(ColorId, color);
                        Block.SetColor(BaseColorId, color);
                    }
                    renderer.SetPropertyBlock(Block, i);
                }
            }
        }

        private static void Clear(GameObject root)
        {
            Block.Clear();
            foreach (Renderer renderer in Paintable(root))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (IsPaintable(materials[i]))
                        renderer.SetPropertyBlock(Block, i);
                }
            }
        }

        private static System.Collections.Generic.IEnumerable<Renderer> Paintable(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                // Mod-built parts can opt out (lamps take the paint as light color instead)
                if ((renderer is MeshRenderer || renderer is SkinnedMeshRenderer) && !renderer.gameObject.name.StartsWith("TF_NoPaint"))
                    yield return renderer;
            }
        }

        private static bool IsPaintable(Material material)
        {
            if (material == null || (!material.HasProperty(ColorId) && !material.HasProperty(BaseColorId)))
                return false;
            // Transparent and overlay materials
            if (material.renderQueue >= 2450)
                return false;

            string name = material.name.ToLowerInvariant();
            foreach (string word in SkippedMaterialWords)
            {
                if (name.Contains(word))
                    return false;
            }
            string shader = material.shader != null ? material.shader.name.ToLowerInvariant() : "";
            foreach (string word in SkippedShaderWords)
            {
                if (shader.Contains(word))
                    return false;
            }
            return true;
        }
    }
}
