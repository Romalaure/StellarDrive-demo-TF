using System;
using System.Collections.Generic;
using UnityEngine;

namespace StellarDriveDemoTF.Paint
{
    internal readonly struct PartKey : IEquatable<PartKey>
    {
        public readonly uint ShipId;
        public readonly ushort PartId;

        public PartKey(uint shipId, ushort partId)
        {
            ShipId = shipId;
            PartId = partId;
        }

        public bool Equals(PartKey other) => ShipId == other.ShipId && PartId == other.PartId;
        public override bool Equals(object obj) => obj is PartKey other && Equals(other);
        public override int GetHashCode() => (int)(ShipId * 397) ^ PartId;
        public override string ToString() => ShipId + ":" + PartId;
    }

    /// <summary>
    /// Surface look of a painted part. Without a finish the part keeps its own shininess and only
    /// takes the color. Values are 0-255 so they travel and save compactly.
    /// </summary>
    internal struct PaintFinish : IEquatable<PaintFinish>
    {
        public bool Enabled;
        public byte Gloss;
        public byte Metal;
        public byte Glow;

        public static PaintFinish None => default;

        public static PaintFinish Of(float gloss, float metal, float glow) => new PaintFinish
        {
            Enabled = true,
            Gloss = ToByte(gloss),
            Metal = ToByte(metal),
            Glow = ToByte(glow)
        };

        public float GlossValue => Gloss / 255f;
        public float MetalValue => Metal / 255f;
        public float GlowValue => Glow / 255f;

        public bool Equals(PaintFinish other) =>
            Enabled == other.Enabled && (!Enabled || (Gloss == other.Gloss && Metal == other.Metal && Glow == other.Glow));

        public override bool Equals(object obj) => obj is PaintFinish other && Equals(other);
        public override int GetHashCode() => Enabled ? (Gloss << 16) | (Metal << 8) | Glow : -1;

        private static byte ToByte(float value) => (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
    }

    internal struct PaintData
    {
        public Color32 Color;
        public PaintFinish Finish;

        public PaintData(Color32 color, PaintFinish finish)
        {
            Color = color;
            Finish = finish;
        }

        public bool SameAs(PaintData other) =>
            Color.r == other.Color.r && Color.g == other.Color.g && Color.b == other.Color.b && Finish.Equals(other.Finish);
    }

    /// <summary>Paint of every painted floating part (doors, machines, ...). Unpainted parts are absent.</summary>
    internal sealed class PaintStore
    {
        private readonly Dictionary<PartKey, PaintData> _paint = new Dictionary<PartKey, PaintData>();

        public int Count => _paint.Count;
        public IEnumerable<KeyValuePair<PartKey, PaintData>> All => _paint;

        public bool TryGet(PartKey key, out PaintData paint) => _paint.TryGetValue(key, out paint);
        public void Set(PartKey key, PaintData paint) => _paint[key] = paint;
        public bool Remove(PartKey key) => _paint.Remove(key);
        public void Clear() => _paint.Clear();
    }
}
