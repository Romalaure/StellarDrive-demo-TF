using System;
using System.Collections.Generic;
using System.Linq;
using StellarDriveDemoTF.Common;
using UnityEngine;

namespace StellarDriveDemoTF.Chairs
{
    internal enum ChairGroup { Frame, Cushion, Trim, Glow }

    /// <summary>Look of one material group of a chair.</summary>
    internal struct ChairMaterial
    {
        public Color Color;
        public float Metallic;
        public float Smoothness;
        public bool Glows;

        public ChairMaterial(Color color, float metallic, float smoothness, bool glows = false)
        {
            Color = color;
            Metallic = metallic;
            Smoothness = smoothness;
            Glows = glows;
        }
    }

    /// <summary>
    /// One chair. Geometry is in part space: y up from the floor, the sitter faces +z. Chairs are
    /// built on the game's pilot seat, which places a seated player's body at (0, 0.69, -0.25) and
    /// head at (0, 1.37, -0.32), so every seat surface sits about 0.5 m high with its back near
    /// z = -0.5.
    /// </summary>
    internal sealed class ChairSpec
    {
        public ushort Id;
        public string Label;
        public string Description;
        public float Mass;
        public (uint Item, int Count)[] Cost;
        public Vector3 BoundsCenter;
        public Vector3 BoundsSize;
        /// <summary>A copilot seat flies the ship like the pilot seat; other chairs are only for sitting.</summary>
        public bool CanPilot;
        public Dictionary<ChairGroup, ChairMaterial> Materials;
        public Action<Dictionary<ChairGroup, MeshBuilder>> Shape;

        public Dictionary<ChairGroup, MeshBuilder> BuildShapes()
        {
            var builders = new Dictionary<ChairGroup, MeshBuilder>();
            foreach (ChairGroup group in Enum.GetValues(typeof(ChairGroup)))
                builders[group] = new MeshBuilder();
            Shape(builders);
            return builders;
        }
    }

    internal static class ChairCatalog
    {
        private const uint Iron = 101, Glass = 102, Aluminum = 104, Wood = 110, Copper = 112;

        private static readonly ChairMaterial Oak = new ChairMaterial(new Color(0.47f, 0.3f, 0.16f), 0f, 0.25f);
        private static readonly ChairMaterial DarkWood = new ChairMaterial(new Color(0.3f, 0.19f, 0.1f), 0f, 0.3f);
        private static readonly ChairMaterial Chrome = new ChairMaterial(new Color(0.85f, 0.86f, 0.89f), 1f, 0.85f);
        private static readonly ChairMaterial Gunmetal = new ChairMaterial(new Color(0.18f, 0.19f, 0.21f), 0.7f, 0.5f);

        public static readonly ChairSpec[] All =
        {
            new ChairSpec
            {
                Id = 7131,
                Label = "Chaise rustique",
                Description = "Chaise en bois massif à barreaux, comme dans une vieille ferme. Assieds-toi dessus comme sur un siège. Peins-la pour la teinter.",
                Mass = 4f,
                Cost = new[] { (Wood, 4) },
                BoundsCenter = new Vector3(0f, 0.53f, -0.25f),
                BoundsSize = new Vector3(0.5f, 1.06f, 0.5f),
                Materials = new Dictionary<ChairGroup, ChairMaterial> { [ChairGroup.Frame] = Oak, [ChairGroup.Trim] = DarkWood },
                Shape = b =>
                {
                    MeshBuilder frame = b[ChairGroup.Frame], trim = b[ChairGroup.Trim];
                    frame.Box(new Vector3(0f, 0.46f, -0.25f), new Vector3(0.48f, 0.045f, 0.47f));
                    // Legs, the back ones running up into the backrest
                    foreach (float x in new[] { -0.2f, 0.2f })
                    {
                        trim.Box(new Vector3(x, 0.22f, -0.04f), new Vector3(0.045f, 0.44f, 0.045f));
                        trim.Box(new Vector3(x, 0.53f, -0.46f), new Vector3(0.045f, 1.06f, 0.045f));
                        trim.Box(new Vector3(x, 0.13f, -0.25f), new Vector3(0.025f, 0.03f, 0.4f));
                    }
                    trim.Box(new Vector3(0f, 0.13f, -0.04f), new Vector3(0.4f, 0.03f, 0.025f));
                    // Spindle back and top rail
                    foreach (float x in new[] { -0.1f, 0f, 0.1f })
                        frame.Box(new Vector3(x, 0.74f, -0.46f), new Vector3(0.035f, 0.5f, 0.02f));
                    frame.Box(new Vector3(0f, 1.0f, -0.46f), new Vector3(0.46f, 0.08f, 0.035f));
                    frame.Box(new Vector3(0f, 0.55f, -0.46f), new Vector3(0.4f, 0.04f, 0.03f));
                }
            },
            new ChairSpec
            {
                Id = 7132,
                Label = "Siège futuriste",
                Description = "Coque blanche sur pied central, coussins sombres et liseré lumineux. Le siège de passager d'un vaisseau de luxe. Peins-le pour changer la couleur de la coque.",
                Mass = 6f,
                Cost = new[] { (Aluminum, 3), (Glass, 1), (Copper, 1) },
                BoundsCenter = new Vector3(0f, 0.6f, -0.25f),
                BoundsSize = new Vector3(0.66f, 1.2f, 0.62f),
                Materials = new Dictionary<ChairGroup, ChairMaterial>
                {
                    [ChairGroup.Frame] = new ChairMaterial(new Color(0.92f, 0.93f, 0.95f), 0.1f, 0.85f),
                    [ChairGroup.Cushion] = new ChairMaterial(new Color(0.12f, 0.13f, 0.15f), 0f, 0.3f),
                    [ChairGroup.Trim] = Chrome,
                    [ChairGroup.Glow] = new ChairMaterial(new Color(0.2f, 0.9f, 1f), 0f, 0.8f, glows: true)
                },
                Shape = b =>
                {
                    MeshBuilder frame = b[ChairGroup.Frame], cushion = b[ChairGroup.Cushion];
                    b[ChairGroup.Frame].Cylinder(Vector3.zero, new Vector3(0f, 0.03f, 0f), 0.26f, 0.23f, 32);
                    b[ChairGroup.Trim].Cylinder(new Vector3(0f, 0.03f, -0.22f), new Vector3(0f, 0.37f, -0.22f), 0.05f, 0.04f, 20);
                    frame.Box(new Vector3(0f, 0.4f, -0.24f), new Vector3(0.54f, 0.07f, 0.52f));
                    cushion.Box(new Vector3(0f, 0.46f, -0.22f), new Vector3(0.46f, 0.05f, 0.44f));
                    (Vector3 up, Vector3 forward) = Tilt(12f);
                    frame.Box(new Vector3(0f, 0.8f, -0.53f), new Vector3(0.54f, 0.74f, 0.07f), Vector3.right, up, forward);
                    cushion.Box(new Vector3(0f, 0.8f, -0.49f), new Vector3(0.44f, 0.62f, 0.04f), Vector3.right, up, forward);
                    foreach (float x in new[] { -0.3f, 0.3f })
                    {
                        frame.Box(new Vector3(x, 0.62f, -0.22f), new Vector3(0.06f, 0.04f, 0.4f));
                        frame.Box(new Vector3(x, 0.52f, -0.38f), new Vector3(0.04f, 0.2f, 0.04f));
                        b[ChairGroup.Glow].Box(new Vector3(x * 0.92f, 0.4f, -0.24f), new Vector3(0.012f, 0.02f, 0.5f));
                    }
                    b[ChairGroup.Glow].Box(new Vector3(0f, 1.16f, -0.6f), new Vector3(0.4f, 0.02f, 0.02f), Vector3.right, up, forward);
                }
            },
            new ChairSpec
            {
                Id = 7133,
                Label = "Siège copilote",
                Description = "Baquet de course avec appui-tête et harnais. C'est un vrai poste de pilotage : assis dedans, tu peux piloter le vaisseau comme depuis le siège pilote.",
                Mass = 8f,
                Cost = new[] { (Iron, 4), (Aluminum, 2), (Copper, 2) },
                BoundsCenter = new Vector3(0f, 0.72f, -0.27f),
                BoundsSize = new Vector3(0.66f, 1.44f, 0.62f),
                CanPilot = true,
                Materials = new Dictionary<ChairGroup, ChairMaterial>
                {
                    [ChairGroup.Frame] = Gunmetal,
                    [ChairGroup.Cushion] = new ChairMaterial(new Color(0.13f, 0.17f, 0.26f), 0f, 0.35f),
                    [ChairGroup.Trim] = new ChairMaterial(new Color(0.98f, 0.5f, 0.12f), 0.2f, 0.5f)
                },
                Shape = b =>
                {
                    MeshBuilder frame = b[ChairGroup.Frame], cushion = b[ChairGroup.Cushion], trim = b[ChairGroup.Trim];
                    frame.Box(new Vector3(0f, 0.04f, -0.25f), new Vector3(0.56f, 0.08f, 0.56f));
                    frame.Box(new Vector3(0f, 0.22f, -0.27f), new Vector3(0.36f, 0.3f, 0.4f));
                    cushion.Box(new Vector3(0f, 0.42f, -0.24f), new Vector3(0.5f, 0.12f, 0.5f));
                    foreach (float x in new[] { -0.24f, 0.24f })
                        cushion.Box(new Vector3(x, 0.51f, -0.24f), new Vector3(0.07f, 0.1f, 0.48f));
                    (Vector3 up, Vector3 forward) = Tilt(10f);
                    cushion.Box(new Vector3(0f, 0.86f, -0.54f), new Vector3(0.52f, 0.74f, 0.12f), Vector3.right, up, forward);
                    foreach (float x in new[] { -0.25f, 0.25f })
                        cushion.Box(new Vector3(x, 0.8f, -0.48f), new Vector3(0.07f, 0.56f, 0.1f), Vector3.right, up, forward);
                    cushion.Box(new Vector3(0f, 1.33f, -0.6f), new Vector3(0.32f, 0.2f, 0.1f), Vector3.right, up, forward);
                    foreach (float x in new[] { -0.1f, 0.1f })
                        trim.Box(new Vector3(x, 0.86f, -0.475f), new Vector3(0.05f, 0.66f, 0.012f), Vector3.right, up, forward);
                    trim.Box(new Vector3(0f, 0.63f, -0.46f), new Vector3(0.3f, 0.05f, 0.012f), Vector3.right, up, forward);
                    foreach (float x in new[] { -0.3f, 0.3f })
                    {
                        frame.Box(new Vector3(x, 0.62f, -0.2f), new Vector3(0.07f, 0.05f, 0.36f));
                        frame.Box(new Vector3(x, 0.5f, -0.33f), new Vector3(0.04f, 0.2f, 0.05f));
                    }
                }
            },
            new ChairSpec
            {
                Id = 7134,
                Label = "Fauteuil club",
                Description = "Gros fauteuil en cuir capitonné sur pieds en bois, pour le salon ou la cabine du capitaine. Peins-le pour changer la couleur du cuir.",
                Mass = 10f,
                Cost = new[] { (Wood, 3), (Iron, 1) },
                BoundsCenter = new Vector3(0f, 0.55f, -0.25f),
                BoundsSize = new Vector3(0.84f, 1.1f, 0.66f),
                Materials = new Dictionary<ChairGroup, ChairMaterial>
                {
                    [ChairGroup.Cushion] = new ChairMaterial(new Color(0.36f, 0.17f, 0.09f), 0f, 0.55f),
                    [ChairGroup.Frame] = DarkWood
                },
                Shape = b =>
                {
                    MeshBuilder cushion = b[ChairGroup.Cushion];
                    foreach (float x in new[] { -0.34f, 0.34f })
                    foreach (float z in new[] { -0.52f, 0.0f })
                        b[ChairGroup.Frame].Cylinder(new Vector3(x, 0f, z), new Vector3(x, 0.1f, z), 0.025f, 0.03f, 12);
                    cushion.Box(new Vector3(0f, 0.26f, -0.26f), new Vector3(0.82f, 0.32f, 0.62f));
                    cushion.Box(new Vector3(0f, 0.47f, -0.22f), new Vector3(0.56f, 0.1f, 0.52f));
                    foreach (float x in new[] { -0.35f, 0.35f })
                        cushion.Box(new Vector3(x, 0.6f, -0.26f), new Vector3(0.13f, 0.36f, 0.62f));
                    cushion.Box(new Vector3(0f, 0.8f, -0.51f), new Vector3(0.82f, 0.6f, 0.14f));
                    // Buttoned back
                    for (int i = 0; i < 3; i++)
                        b[ChairGroup.Frame].Box(new Vector3(-0.15f + i * 0.15f, 0.82f, -0.438f), new Vector3(0.02f, 0.02f, 0.01f));
                }
            },
            new ChairSpec
            {
                Id = 7135,
                Label = "Tabouret de bar",
                Description = "Tabouret haut sur pied chromé avec assise ronde rembourrée, pour un comptoir ou un poste de travail. Peins-le pour changer la couleur de l'assise.",
                Mass = 2.5f,
                Cost = new[] { (Iron, 1), (Aluminum, 1) },
                BoundsCenter = new Vector3(0f, 0.26f, -0.22f),
                BoundsSize = new Vector3(0.42f, 0.52f, 0.42f),
                Materials = new Dictionary<ChairGroup, ChairMaterial>
                {
                    [ChairGroup.Frame] = Chrome,
                    [ChairGroup.Cushion] = new ChairMaterial(new Color(0.65f, 0.08f, 0.07f), 0f, 0.6f)
                },
                Shape = b =>
                {
                    var c = new Vector3(0f, 0f, -0.22f);
                    b[ChairGroup.Frame].Cylinder(c, c + new Vector3(0f, 0.02f, 0f), 0.2f, 0.19f, 28);
                    b[ChairGroup.Frame].Cylinder(c + new Vector3(0f, 0.02f, 0f), c + new Vector3(0f, 0.42f, 0f), 0.03f, 0.03f, 16);
                    b[ChairGroup.Frame].Cylinder(c + new Vector3(0f, 0.2f, 0f), c + new Vector3(0f, 0.215f, 0f), 0.14f, 0.14f, 24);
                    b[ChairGroup.Cushion].Cylinder(c + new Vector3(0f, 0.42f, 0f), c + new Vector3(0f, 0.5f, 0f), 0.19f, 0.18f, 28);
                }
            },
            new ChairSpec
            {
                Id = 7136,
                Label = "Banquette",
                Description = "Banc d'1,20 m en bois avec coussin et dossier, pour un réfectoire ou une salle d'attente. On s'assoit au milieu. Peins-la pour la teinter.",
                Mass = 9f,
                Cost = new[] { (Wood, 5), (Iron, 1) },
                BoundsCenter = new Vector3(0f, 0.45f, -0.27f),
                BoundsSize = new Vector3(1.2f, 0.9f, 0.56f),
                Materials = new Dictionary<ChairGroup, ChairMaterial>
                {
                    [ChairGroup.Frame] = Oak,
                    [ChairGroup.Cushion] = new ChairMaterial(new Color(0.25f, 0.32f, 0.24f), 0f, 0.3f),
                    [ChairGroup.Trim] = Gunmetal
                },
                Shape = b =>
                {
                    b[ChairGroup.Frame].Box(new Vector3(0f, 0.42f, -0.25f), new Vector3(1.2f, 0.05f, 0.5f));
                    b[ChairGroup.Cushion].Box(new Vector3(0f, 0.47f, -0.23f), new Vector3(1.14f, 0.05f, 0.44f));
                    foreach (float x in new[] { -0.55f, 0.55f })
                    {
                        b[ChairGroup.Trim].Box(new Vector3(x, 0.2f, -0.25f), new Vector3(0.05f, 0.4f, 0.44f));
                        b[ChairGroup.Trim].Box(new Vector3(x, 0.66f, -0.5f), new Vector3(0.04f, 0.44f, 0.04f));
                    }
                    (Vector3 up, Vector3 forward) = Tilt(8f);
                    foreach (float y in new[] { 0.62f, 0.78f })
                        b[ChairGroup.Frame].Box(new Vector3(0f, y, -0.5f + (y - 0.62f) * -0.14f), new Vector3(1.16f, 0.11f, 0.03f), Vector3.right, up, forward);
                }
            },
            new ChairSpec
            {
                Id = 7137,
                Label = "Siège de bureau",
                Description = "Chaise de bureau à roulettes, piètement étoile et dossier en résille, pour la salle des machines ou le poste de navigation. Peins-la pour la teinter.",
                Mass = 5f,
                Cost = new[] { (Aluminum, 2), (Iron, 1), (Copper, 1) },
                BoundsCenter = new Vector3(0f, 0.55f, -0.24f),
                BoundsSize = new Vector3(0.62f, 1.1f, 0.62f),
                Materials = new Dictionary<ChairGroup, ChairMaterial>
                {
                    [ChairGroup.Frame] = new ChairMaterial(new Color(0.08f, 0.08f, 0.09f), 0.2f, 0.45f),
                    [ChairGroup.Cushion] = new ChairMaterial(new Color(0.2f, 0.21f, 0.23f), 0f, 0.2f),
                    [ChairGroup.Trim] = Chrome
                },
                Shape = b =>
                {
                    var c = new Vector3(0f, 0f, -0.24f);
                    // Five-star base with casters
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * Mathf.PI * 2f / 5f;
                        var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                        b[ChairGroup.Frame].Box(c + dir * 0.15f + new Vector3(0f, 0.08f, 0f), new Vector3(0.05f, 0.035f, 0.3f), Vector3.Cross(Vector3.up, dir), Vector3.up, dir);
                        Vector3 wheel = c + dir * 0.29f;
                        b[ChairGroup.Trim].Cylinder(wheel + new Vector3(0f, 0.03f, 0f), wheel + new Vector3(0f, 0.065f, 0f), 0.012f, 0.012f, 8);
                        b[ChairGroup.Frame].Cylinder(wheel - Vector3.Cross(Vector3.up, dir) * 0.015f + new Vector3(0f, 0.03f, 0f),
                            wheel + Vector3.Cross(Vector3.up, dir) * 0.015f + new Vector3(0f, 0.03f, 0f), 0.03f, 0.03f, 12);
                    }
                    b[ChairGroup.Trim].Cylinder(c + new Vector3(0f, 0.09f, 0f), c + new Vector3(0f, 0.4f, 0f), 0.025f, 0.025f, 16);
                    b[ChairGroup.Frame].Box(c + new Vector3(0f, 0.41f, 0.02f), new Vector3(0.48f, 0.04f, 0.46f));
                    b[ChairGroup.Cushion].Box(c + new Vector3(0f, 0.46f, 0.03f), new Vector3(0.48f, 0.06f, 0.46f));
                    (Vector3 up, Vector3 forward) = Tilt(10f);
                    b[ChairGroup.Frame].Box(new Vector3(0f, 0.6f, -0.5f), new Vector3(0.06f, 0.28f, 0.04f), Vector3.right, up, forward);
                    b[ChairGroup.Frame].Box(new Vector3(0f, 0.86f, -0.52f), new Vector3(0.46f, 0.5f, 0.04f), Vector3.right, up, forward);
                    b[ChairGroup.Cushion].Box(new Vector3(0f, 0.86f, -0.495f), new Vector3(0.4f, 0.44f, 0.015f), Vector3.right, up, forward);
                    foreach (float x in new[] { -0.27f, 0.27f })
                    {
                        b[ChairGroup.Frame].Box(new Vector3(x, 0.55f, -0.3f), new Vector3(0.03f, 0.16f, 0.03f));
                        b[ChairGroup.Frame].Box(new Vector3(x, 0.64f, -0.25f), new Vector3(0.06f, 0.03f, 0.24f));
                    }
                }
            },
            new ChairSpec
            {
                Id = 7138,
                Label = "Siège passager",
                Description = "Siège de navette avec appui-tête et accoudoirs, à aligner en rangées dans une cabine de transport. Peins-le pour changer la couleur du tissu.",
                Mass = 7f,
                Cost = new[] { (Aluminum, 2), (Iron, 2) },
                BoundsCenter = new Vector3(0f, 0.65f, -0.26f),
                BoundsSize = new Vector3(0.6f, 1.3f, 0.6f),
                Materials = new Dictionary<ChairGroup, ChairMaterial>
                {
                    [ChairGroup.Frame] = new ChairMaterial(new Color(0.6f, 0.62f, 0.65f), 0.8f, 0.6f),
                    [ChairGroup.Cushion] = new ChairMaterial(new Color(0.1f, 0.2f, 0.45f), 0f, 0.25f),
                    [ChairGroup.Trim] = new ChairMaterial(new Color(0.9f, 0.9f, 0.9f), 0f, 0.3f)
                },
                Shape = b =>
                {
                    foreach (float x in new[] { -0.22f, 0.22f })
                    {
                        b[ChairGroup.Frame].Box(new Vector3(x, 0.02f, -0.27f), new Vector3(0.05f, 0.04f, 0.5f));
                        b[ChairGroup.Frame].Box(new Vector3(x, 0.2f, -0.1f), new Vector3(0.04f, 0.36f, 0.04f));
                        b[ChairGroup.Frame].Box(new Vector3(x, 0.2f, -0.42f), new Vector3(0.04f, 0.36f, 0.04f));
                        b[ChairGroup.Frame].Box(new Vector3(x * 1.25f, 0.62f, -0.25f), new Vector3(0.05f, 0.04f, 0.42f));
                        b[ChairGroup.Frame].Box(new Vector3(x * 1.25f, 0.52f, -0.42f), new Vector3(0.04f, 0.18f, 0.04f));
                    }
                    b[ChairGroup.Frame].Box(new Vector3(0f, 0.39f, -0.26f), new Vector3(0.5f, 0.04f, 0.48f));
                    b[ChairGroup.Cushion].Box(new Vector3(0f, 0.45f, -0.24f), new Vector3(0.48f, 0.09f, 0.48f));
                    (Vector3 up, Vector3 forward) = Tilt(12f);
                    b[ChairGroup.Cushion].Box(new Vector3(0f, 0.86f, -0.53f), new Vector3(0.48f, 0.72f, 0.12f), Vector3.right, up, forward);
                    b[ChairGroup.Trim].Box(new Vector3(0f, 1.18f, -0.535f), new Vector3(0.36f, 0.16f, 0.13f), Vector3.right, up, forward);
                }
            }
        };

        private static readonly HashSet<ushort> Ids = new HashSet<ushort>(All.Select(c => c.Id));
        private static readonly HashSet<ushort> PilotIds = new HashSet<ushort>(All.Where(c => c.CanPilot).Select(c => c.Id));

        public static bool IsChair(ushort partId) => Ids.Contains(partId);

        /// <summary>
        /// Chairs you can only sit on: they must not affect the ship in any way. Every chair is,
        /// except the copilot seat when the CopilotCanFly setting allows it to fly.
        /// </summary>
        public static bool IsSitOnly(ushort partId) =>
            Ids.Contains(partId) && !(PilotIds.Contains(partId) && Settings.CopilotCanFly.Value);

        /// <summary>Back tilted by the given angle: its up axis leans toward -z.</summary>
        private static (Vector3 Up, Vector3 Forward) Tilt(float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return (new Vector3(0f, Mathf.Cos(r), -Mathf.Sin(r)), new Vector3(0f, Mathf.Sin(r), Mathf.Cos(r)));
        }

        /// <summary>All chairs side by side, for previewing models outside the game.</summary>
        internal static Dictionary<ChairGroup, MeshBuilder> BuildShapes()
        {
            var merged = new Dictionary<ChairGroup, MeshBuilder>();
            foreach (ChairGroup group in Enum.GetValues(typeof(ChairGroup)))
                merged[group] = new MeshBuilder();
            float x = 0f;
            foreach (ChairSpec chair in All)
            {
                foreach (KeyValuePair<ChairGroup, MeshBuilder> entry in chair.BuildShapes())
                    merged[entry.Key].Append(entry.Value, new Vector3(x + chair.BoundsSize.x / 2f, 0f, 0f));
                x += chair.BoundsSize.x + 0.3f;
            }
            return merged;
        }
    }
}
