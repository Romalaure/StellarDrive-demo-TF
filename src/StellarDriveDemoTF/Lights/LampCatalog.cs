using System.Collections.Generic;
using System.Linq;
using StellarDriveDemoTF.Common;
using UnityEngine;

namespace StellarDriveDemoTF.Lights
{
    internal enum LampGroup { Frame, Trim, Diffuser }

    /// <summary>One placeable light. Geometry is in part space: y points out of the surface it is mounted on.</summary>
    internal sealed class LampSpec
    {
        public ushort Id;
        public string Label;
        public string Description;
        public float Mass;
        public (uint Item, int Count)[] Cost;
        public Vector3 BoundsCenter;
        public Vector3 BoundsSize;
        public Vector3 SocketPosition;
        public LightType LightType;
        public Vector3 LightPosition;
        /// <summary>Where a spot shines, in part space. Out of the mounting surface by default.</summary>
        public Vector3 LightDirection = Vector3.up;
        public float Range;
        public float Intensity;
        public float SpotAngle;
        public System.Action<Dictionary<LampGroup, MeshBuilder>> Shape;

        public Dictionary<LampGroup, MeshBuilder> BuildShapes()
        {
            var builders = new Dictionary<LampGroup, MeshBuilder>
            {
                [LampGroup.Frame] = new MeshBuilder(),
                [LampGroup.Trim] = new MeshBuilder(),
                [LampGroup.Diffuser] = new MeshBuilder()
            };
            Shape(builders);
            return builders;
        }
    }

    internal static class LampCatalog
    {
        private const uint Iron = 101;
        private const uint Glass = 102;
        private const uint Copper = 112;

        public static readonly Color DefaultLight = new Color(1f, 0.9f, 0.75f);

        /// <summary>
        /// Direction the sloped face of a corner wedge looks: away from the surface and from the wall.
        /// Declared before All, which uses it while initializing.
        /// </summary>
        private static readonly Vector3 CornerNormal = new Vector3(0f, 1f, -1f).normalized;

        public static readonly LampSpec[] All =
        {
            new LampSpec
            {
                Id = 7101,
                Label = "Plafonnier",
                Description = "Lampe ronde à fixer au plafond. Allumée tant qu'aucun câble de signal n'est branché ; branchée, elle suit le signal (0 = éteinte, 1 = pleine puissance). Peins-la pour changer la couleur de la lumière.",
                Mass = 2f,
                Cost = new[] { (Iron, 2), (Glass, 1), (Copper, 1) },
                BoundsCenter = new Vector3(0f, 0.035f, 0f),
                BoundsSize = new Vector3(0.44f, 0.07f, 0.44f),
                SocketPosition = new Vector3(-0.22f, 0.02f, 0f),
                LightType = LightType.Point,
                LightPosition = new Vector3(0f, 0.3f, 0f),
                Range = 8f,
                Intensity = 2.2f,
                Shape = b =>
                {
                    b[LampGroup.Frame].Cylinder(Vector3.zero, new Vector3(0f, 0.025f, 0f), 0.22f, 0.21f, 28);
                    b[LampGroup.Trim].Cylinder(new Vector3(0f, 0.025f, 0f), new Vector3(0f, 0.035f, 0f), 0.205f, 0.198f, 28);
                    b[LampGroup.Diffuser].Cylinder(new Vector3(0f, 0.035f, 0f), new Vector3(0f, 0.058f, 0f), 0.19f, 0.17f, 28);
                    b[LampGroup.Diffuser].Cylinder(new Vector3(0f, 0.058f, 0f), new Vector3(0f, 0.07f, 0f), 0.17f, 0.12f, 28);
                }
            },
            new LampSpec
            {
                Id = 7102,
                Label = "Applique murale",
                Description = "Petite lampe cubique pour les murs. Allumée tant qu'aucun câble de signal n'est branché ; branchée, elle suit le signal. Peins-la pour changer la couleur de la lumière.",
                Mass = 1.5f,
                Cost = new[] { (Iron, 1), (Glass, 1), (Copper, 1) },
                BoundsCenter = new Vector3(0f, 0.055f, 0f),
                BoundsSize = new Vector3(0.2f, 0.11f, 0.2f),
                SocketPosition = new Vector3(-0.1f, 0.01f, 0f),
                LightType = LightType.Point,
                LightPosition = new Vector3(0f, 0.25f, 0f),
                Range = 6f,
                Intensity = 1.6f,
                Shape = b =>
                {
                    b[LampGroup.Frame].Box(new Vector3(0f, 0.01f, 0f), new Vector3(0.2f, 0.02f, 0.2f));
                    b[LampGroup.Frame].Box(new Vector3(0f, 0.03f, 0f), new Vector3(0.14f, 0.02f, 0.14f));
                    b[LampGroup.Diffuser].Box(new Vector3(0f, 0.07f, 0f), new Vector3(0.12f, 0.06f, 0.12f));
                    b[LampGroup.Trim].Box(new Vector3(0f, 0.105f, 0f), new Vector3(0.135f, 0.01f, 0.135f));
                    // Corner posts holding the cap
                    foreach (float x in new[] { -0.062f, 0.062f })
                    foreach (float z in new[] { -0.062f, 0.062f })
                        b[LampGroup.Trim].Box(new Vector3(x, 0.07f, z), new Vector3(0.012f, 0.06f, 0.012f));
                }
            },
            new LampSpec
            {
                Id = 7103,
                Label = "Projecteur",
                Description = "Phare puissant qui éclaire droit devant lui, à poser sur la coque extérieure, un mur ou le sol. Allumé tant qu'aucun câble de signal n'est branché ; branché, il suit le signal. Peins-le pour changer la couleur du faisceau.",
                Mass = 4f,
                Cost = new[] { (Iron, 3), (Glass, 2), (Copper, 2) },
                BoundsCenter = new Vector3(0f, 0.15f, 0f),
                BoundsSize = new Vector3(0.26f, 0.3f, 0.26f),
                SocketPosition = new Vector3(-0.13f, 0.02f, 0f),
                LightType = LightType.Spot,
                LightPosition = new Vector3(0f, 0.3f, 0f),
                Range = 45f,
                Intensity = 9f,
                SpotAngle = 40f,
                Shape = b =>
                {
                    b[LampGroup.Frame].Cylinder(Vector3.zero, new Vector3(0f, 0.02f, 0f), 0.12f, 0.11f, 20);
                    // Yoke
                    b[LampGroup.Frame].Box(new Vector3(0.108f, 0.085f, 0f), new Vector3(0.016f, 0.13f, 0.05f));
                    b[LampGroup.Frame].Box(new Vector3(-0.108f, 0.085f, 0f), new Vector3(0.016f, 0.13f, 0.05f));
                    b[LampGroup.Trim].Cylinder(new Vector3(-0.118f, 0.14f, 0f), new Vector3(0.118f, 0.14f, 0f), 0.014f, 0.014f, 12);
                    // Housing with cooling rings, chrome bezel and lens
                    b[LampGroup.Frame].Cylinder(new Vector3(0f, 0.05f, 0f), new Vector3(0f, 0.27f, 0f), 0.075f, 0.092f, 24);
                    foreach (float y in new[] { 0.08f, 0.11f, 0.14f })
                        b[LampGroup.Trim].Cylinder(new Vector3(0f, y, 0f), new Vector3(0f, y + 0.008f, 0f), 0.084f, 0.086f, 24);
                    b[LampGroup.Trim].Cylinder(new Vector3(0f, 0.27f, 0f), new Vector3(0f, 0.29f, 0f), 0.1f, 0.1f, 24);
                    b[LampGroup.Diffuser].Cylinder(new Vector3(0f, 0.282f, 0f), new Vector3(0f, 0.292f, 0f), 0.088f, 0.085f, 24);
                }
            },
            new LampSpec
            {
                Id = 7104,
                Label = "Tube néon",
                Description = "Réglette lumineuse d'un mètre pour couloirs, plafonds et coques. Allumée tant qu'aucun câble de signal n'est branché ; branchée, elle suit le signal. Peins-la pour changer la couleur du néon.",
                Mass = 1.5f,
                Cost = new[] { (Iron, 1), (Glass, 2), (Copper, 1) },
                BoundsCenter = new Vector3(0f, 0.035f, 0f),
                BoundsSize = new Vector3(1f, 0.07f, 0.08f),
                SocketPosition = new Vector3(-0.5f, 0.015f, 0f),
                LightType = LightType.Point,
                LightPosition = new Vector3(0f, 0.25f, 0f),
                Range = 6f,
                Intensity = 1.8f,
                Shape = b =>
                {
                    b[LampGroup.Frame].Box(new Vector3(0f, 0.01f, 0f), new Vector3(1f, 0.02f, 0.06f));
                    b[LampGroup.Diffuser].Cylinder(new Vector3(-0.47f, 0.045f, 0f), new Vector3(0.47f, 0.045f, 0f), 0.022f, 0.022f, 16, false, false);
                    b[LampGroup.Trim].Cylinder(new Vector3(-0.5f, 0.045f, 0f), new Vector3(-0.47f, 0.045f, 0f), 0.027f, 0.027f, 16);
                    b[LampGroup.Trim].Cylinder(new Vector3(0.47f, 0.045f, 0f), new Vector3(0.5f, 0.045f, 0f), 0.027f, 0.027f, 16);
                    foreach (float x in new[] { -0.3f, 0f, 0.3f })
                        b[LampGroup.Trim].Box(new Vector3(x, 0.03f, 0f), new Vector3(0.02f, 0.03f, 0.05f));
                }
            },

            // Corner fixtures: a wedge that fills the angle between the surface it is placed on and
            // the wall behind it (+z side), its lit face sloped at 45 degrees into the room
            new LampSpec
            {
                Id = 7105,
                Label = "Lampe d'angle",
                Description = "Lampe en biseau qui s'emboîte dans un coin, entre le plafond (ou le sol) et un mur : pose-la contre le mur, dos au mur, et elle éclaire la pièce en diagonale. Allumée tant qu'aucun câble de signal n'est branché ; branchée, elle suit le signal. Peins-la pour changer la couleur de la lumière.",
                Mass = 1.5f,
                Cost = new[] { (Iron, 1), (Glass, 1), (Copper, 1) },
                BoundsCenter = new Vector3(0f, 0.07f, 0f),
                BoundsSize = new Vector3(0.41f, 0.14f, 0.14f),
                SocketPosition = new Vector3(-0.2f, 0.02f, 0f),
                LightType = LightType.Point,
                LightPosition = CornerLight(0.14f, 0.3f),
                Range = 6f,
                Intensity = 1.6f,
                Shape = b => CornerShape(b, 0.4f, 0.14f)
            },
            new LampSpec
            {
                Id = 7106,
                Label = "Néon d'angle",
                Description = "Réglette d'un mètre en biseau pour les coins entre plafond et mur ou entre sol et mur : pose-la dos au mur. Idéale pour éclairer un couloir sans lampe qui dépasse. Allumée tant qu'aucun câble de signal n'est branché ; branchée, elle suit le signal. Peins-la pour changer la couleur du néon.",
                Mass = 1.5f,
                Cost = new[] { (Iron, 1), (Glass, 2), (Copper, 1) },
                BoundsCenter = new Vector3(0f, 0.045f, 0f),
                BoundsSize = new Vector3(1f, 0.09f, 0.09f),
                SocketPosition = new Vector3(-0.5f, 0.015f, 0f),
                LightType = LightType.Point,
                LightPosition = CornerLight(0.09f, 0.25f),
                Range = 6f,
                Intensity = 1.8f,
                Shape = b => CornerShape(b, 1f, 0.09f)
            },
            new LampSpec
            {
                Id = 7107,
                Label = "Projecteur d'angle",
                Description = "Projecteur fixé dans un coin, dos au mur, qui éclaire en diagonale vers le centre de la pièce ou le long de la coque. Allumé tant qu'aucun câble de signal n'est branché ; branché, il suit le signal. Peins-le pour changer la couleur du faisceau.",
                Mass = 3f,
                Cost = new[] { (Iron, 2), (Glass, 2), (Copper, 2) },
                BoundsCenter = new Vector3(0f, 0.12f, -0.04f),
                BoundsSize = new Vector3(0.2f, 0.24f, 0.24f),
                SocketPosition = new Vector3(-0.1f, 0.02f, 0f),
                LightType = LightType.Spot,
                LightPosition = CornerLight(0.16f, 0.16f),
                LightDirection = CornerNormal,
                Range = 30f,
                Intensity = 6f,
                SpotAngle = 60f,
                Shape = b =>
                {
                    const float size = 0.16f;
                    b[LampGroup.Frame].Wedge(-0.1f, 0.1f, 0f, size, -size / 2f, size / 2f);
                    Vector3 face = new Vector3(0f, size / 2f, 0f);
                    Vector3 lensEnd = face + CornerNormal * 0.13f;
                    b[LampGroup.Frame].Cylinder(face, lensEnd, 0.058f, 0.07f, 20);
                    b[LampGroup.Trim].Cylinder(face + CornerNormal * 0.05f, face + CornerNormal * 0.058f, 0.068f, 0.068f, 20);
                    b[LampGroup.Trim].Cylinder(lensEnd, lensEnd + CornerNormal * 0.015f, 0.078f, 0.078f, 20);
                    b[LampGroup.Diffuser].Cylinder(lensEnd + CornerNormal * 0.008f, lensEnd + CornerNormal * 0.018f, 0.066f, 0.063f, 20);
                }
            }
        };

        private static Vector3 CornerLight(float size, float offset) => new Vector3(0f, size / 2f, 0f) + CornerNormal * offset;

        // Wedge body with chrome end caps and a glowing strip along its sloped face
        private static void CornerShape(Dictionary<LampGroup, MeshBuilder> b, float length, float size)
        {
            float half = length / 2f, z0 = -size / 2f, z1 = size / 2f;
            const float cap = 0.012f, lip = 0.005f;
            b[LampGroup.Frame].Wedge(-half + cap, half - cap, 0f, size, z0, z1);
            b[LampGroup.Trim].Wedge(-half, -half + cap, 0f, size + lip, z0 - lip, z1);
            b[LampGroup.Trim].Wedge(half - cap, half, 0f, size + lip, z0 - lip, z1);

            float slope = Mathf.Sqrt(2f) * size;
            var center = new Vector3(0f, size / 2f, 0f) + CornerNormal * 0.004f;
            b[LampGroup.Diffuser].Box(center, new Vector3(length - 2f * cap - 0.01f, 0.008f, slope * 0.7f),
                Vector3.right, CornerNormal, new Vector3(0f, 1f, 1f).normalized);
        }

        private static readonly HashSet<ushort> Ids = new HashSet<ushort>(All.Select(l => l.Id));

        public static bool IsLamp(ushort partId) => Ids.Contains(partId);

        /// <summary>All lamps side by side, for previewing models outside the game.</summary>
        internal static Dictionary<LampGroup, MeshBuilder> BuildShapes()
        {
            var merged = new Dictionary<LampGroup, MeshBuilder>
            {
                [LampGroup.Frame] = new MeshBuilder(),
                [LampGroup.Trim] = new MeshBuilder(),
                [LampGroup.Diffuser] = new MeshBuilder()
            };
            float x = 0f;
            foreach (LampSpec lamp in All)
            {
                foreach (KeyValuePair<LampGroup, MeshBuilder> entry in lamp.BuildShapes())
                    merged[entry.Key].Append(entry.Value, new Vector3(x + lamp.BoundsSize.x / 2f, 0f, 0f));
                x += lamp.BoundsSize.x + 0.15f;
            }
            return merged;
        }
    }
}
