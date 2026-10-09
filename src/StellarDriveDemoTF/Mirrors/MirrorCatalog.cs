using System.Collections.Generic;
using System.Linq;
using StellarDriveDemoTF.Common;
using UnityEngine;

namespace StellarDriveDemoTF.Mirrors
{
    internal enum MirrorGroup { Frame, Trim }

    /// <summary>One placeable mirror. Part space: y points out of the mounting surface, the glass faces +y.</summary>
    internal sealed class MirrorSpec
    {
        public ushort Id;
        public string Label;
        public string Description;
        public float Mass;
        public (uint Item, int Count)[] Cost;
        public Vector3 BoundsCenter;
        public Vector3 BoundsSize;
        public Vector3 SocketPosition;
        /// <summary>Glass size along local x and z.</summary>
        public Vector2 GlassSize;
        /// <summary>Height of the glass above the mounting surface.</summary>
        public float GlassHeight;
        public System.Action<Dictionary<MirrorGroup, MeshBuilder>> Shape;

        public Dictionary<MirrorGroup, MeshBuilder> BuildShapes()
        {
            var builders = new Dictionary<MirrorGroup, MeshBuilder>
            {
                [MirrorGroup.Frame] = new MeshBuilder(),
                [MirrorGroup.Trim] = new MeshBuilder()
            };
            Shape(builders);
            return builders;
        }
    }

    internal static class MirrorCatalog
    {
        private const uint Iron = 101;
        private const uint Glass = 102;
        private const uint Aluminum = 104;

        public static readonly MirrorSpec[] All =
        {
            new MirrorSpec
            {
                Id = 7111,
                Label = "Rétroviseur",
                Description = "Miroir de cockpit sur un petit bras. Fixe-le au mur devant le siège pilote, au-dessus du pare-brise : il montre ce qu'il y a derrière toi, en temps réel.",
                Mass = 1.5f,
                Cost = new[] { (Iron, 1), (Glass, 2), (Aluminum, 1) },
                GlassSize = new Vector2(0.46f, 0.18f),
                GlassHeight = 0.112f,
                BoundsCenter = new Vector3(0f, 0.06f, 0f),
                BoundsSize = new Vector3(0.52f, 0.12f, 0.24f),
                SocketPosition = new Vector3(-0.26f, 0.01f, 0f),
                Shape = b =>
                {
                    // Mount, arm and housing behind the glass
                    b[MirrorGroup.Frame].Cylinder(Vector3.zero, new Vector3(0f, 0.012f, 0f), 0.045f, 0.04f, 16);
                    b[MirrorGroup.Trim].Cylinder(new Vector3(0f, 0.012f, 0f), new Vector3(0f, 0.075f, 0f), 0.014f, 0.012f, 12);
                    b[MirrorGroup.Trim].Cylinder(new Vector3(-0.02f, 0.075f, 0f), new Vector3(0.02f, 0.075f, 0f), 0.016f, 0.016f, 12);
                    b[MirrorGroup.Frame].Box(new Vector3(0f, 0.095f, 0f), new Vector3(0.5f, 0.032f, 0.22f));
                    // Lip around the glass
                    b[MirrorGroup.Frame].Box(new Vector3(0f, 0.114f, 0.1f), new Vector3(0.5f, 0.006f, 0.02f));
                    b[MirrorGroup.Frame].Box(new Vector3(0f, 0.114f, -0.1f), new Vector3(0.5f, 0.006f, 0.02f));
                    b[MirrorGroup.Frame].Box(new Vector3(0.24f, 0.114f, 0f), new Vector3(0.02f, 0.006f, 0.22f));
                    b[MirrorGroup.Frame].Box(new Vector3(-0.24f, 0.114f, 0f), new Vector3(0.02f, 0.006f, 0.22f));
                }
            },
            new MirrorSpec
            {
                Id = 7112,
                Label = "Grand miroir",
                Description = "Miroir mural en pied qui reflète la pièce en temps réel. Pratique dans une cabine, ou pour surveiller une porte.",
                Mass = 6f,
                Cost = new[] { (Iron, 2), (Glass, 4), (Aluminum, 2) },
                GlassSize = new Vector2(0.8f, 1.2f),
                GlassHeight = 0.032f,
                BoundsCenter = new Vector3(0f, 0.02f, 0f),
                BoundsSize = new Vector3(0.88f, 0.04f, 1.28f),
                SocketPosition = new Vector3(-0.44f, 0.01f, 0f),
                Shape = b =>
                {
                    b[MirrorGroup.Frame].Box(new Vector3(0f, 0.012f, 0f), new Vector3(0.84f, 0.024f, 1.24f));
                    b[MirrorGroup.Trim].Box(new Vector3(0f, 0.034f, 0.62f), new Vector3(0.88f, 0.012f, 0.04f));
                    b[MirrorGroup.Trim].Box(new Vector3(0f, 0.034f, -0.62f), new Vector3(0.88f, 0.012f, 0.04f));
                    b[MirrorGroup.Trim].Box(new Vector3(0.42f, 0.034f, 0f), new Vector3(0.04f, 0.012f, 1.2f));
                    b[MirrorGroup.Trim].Box(new Vector3(-0.42f, 0.034f, 0f), new Vector3(0.04f, 0.012f, 1.2f));
                }
            }
        };

        private static readonly HashSet<ushort> Ids = new HashSet<ushort>(All.Select(m => m.Id));

        public static bool IsMirror(ushort partId) => Ids.Contains(partId);

        /// <summary>Glass quad facing +y. UVs are mirrored left to right, as a reflection is.</summary>
        public static Mesh BuildGlass(MirrorSpec spec)
        {
            float w = spec.GlassSize.x;
            float h = spec.GlassSize.y;
            float y = spec.GlassHeight;
            var mesh = new Mesh { name = "TF_MirrorGlass" + spec.Id };
            mesh.vertices = new[]
            {
                new Vector3(-w / 2f, y, -h / 2f), new Vector3(w / 2f, y, -h / 2f),
                new Vector3(-w / 2f, y, h / 2f), new Vector3(w / 2f, y, h / 2f)
            };
            // The mirror camera's right is local -x (see MirrorVisuals), so u runs from +x to -x
            mesh.uv = new[] { new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            // Front faces: Cross(p1 - p0, p2 - p0) must point +y
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        /// <summary>All mirrors side by side with their glass, for previewing outside the game.</summary>
        internal static Dictionary<string, MeshBuilder> BuildShapes()
        {
            var merged = new Dictionary<string, MeshBuilder> { ["Frame"] = new MeshBuilder(), ["Trim"] = new MeshBuilder(), ["Glass"] = new MeshBuilder() };
            float x = 0f;
            foreach (MirrorSpec mirror in All)
            {
                var offset = new Vector3(x + mirror.BoundsSize.x / 2f, 0f, 0f);
                foreach (KeyValuePair<MirrorGroup, MeshBuilder> entry in mirror.BuildShapes())
                    merged[entry.Key.ToString()].Append(entry.Value, offset);
                var glass = new MeshBuilder();
                glass.Box(new Vector3(0f, mirror.GlassHeight, 0f), new Vector3(mirror.GlassSize.x, 0.002f, mirror.GlassSize.y));
                merged["Glass"].Append(glass, offset);
                x += mirror.BoundsSize.x + 0.2f;
            }
            return merged;
        }
    }
}
