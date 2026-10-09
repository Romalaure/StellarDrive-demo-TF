using Ships.Parts.Common.Model.Reactions;
using StellarDriveDemoTF.Common;
using UnityEngine;

namespace StellarDriveDemoTF.Paint
{
    /// <summary>
    /// Applies paint to a part's visual by overriding the base color of its opaque materials
    /// through property blocks, so shared materials stay untouched.
    /// </summary>
    internal static class PartTint
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColor = Shader.PropertyToID("_Color");

        // Materials whose look depends on their own color: glass, screens, holograms, lights
        private static readonly string[] SkippedMaterialWords = { "glass", "screen", "display", "holo", "emissi", "light", "lamp", "fluid", "liquid" };

        private static readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();

        /// <summary>Re-applies the stored color (or removes the tint) of a part, if its visual is loaded.</summary>
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
            if (PaintNet.Client.TryGet(key, out Color32 color))
                Apply(root, (Color)color);
            else
                Apply(root, null);
        }

        private static void Apply(GameObject root, Color? color)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                    continue;

                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (!IsPaintable(materials[i]))
                        continue;

                    renderer.GetPropertyBlock(Block, i);
                    if (color.HasValue)
                    {
                        Block.SetColor(BaseColor, color.Value);
                        Block.SetColor(LegacyColor, color.Value);
                        renderer.SetPropertyBlock(Block, i);
                    }
                    else
                    {
                        Block.Clear();
                        renderer.SetPropertyBlock(Block, i);
                    }
                }
            }
        }

        private static bool IsPaintable(Material material)
        {
            if (material == null || !material.HasProperty(BaseColor))
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
            return true;
        }
    }
}
