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

    /// <summary>Paint color of every painted floating part (doors, machines, ...). Unpainted parts are absent.</summary>
    internal sealed class PaintStore
    {
        private readonly Dictionary<PartKey, Color32> _colors = new Dictionary<PartKey, Color32>();

        public int Count => _colors.Count;
        public IEnumerable<KeyValuePair<PartKey, Color32>> All => _colors;

        public bool TryGet(PartKey key, out Color32 color) => _colors.TryGetValue(key, out color);
        public void Set(PartKey key, Color32 color) => _colors[key] = color;
        public bool Remove(PartKey key) => _colors.Remove(key);
        public void Clear() => _colors.Clear();

        public static bool SameRgb(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b;
    }
}
