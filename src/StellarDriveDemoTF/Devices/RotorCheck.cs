using System.Collections.Generic;
using System.Linq;
using Ships;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.State;
using StellarDriveDemoTF.Common;
using UnityEngine;

namespace StellarDriveDemoTF.Devices
{
    /// <summary>
    /// Schematics saved before rotors were handled built rotors that took hold of the original
    /// ship's turning part: two rotors then drive the same part and its thrusters go wrong. On the
    /// host, this looks for rotors holding the same turning part and tells the player which one to
    /// recycle (the copy's: the ship with the larger number, built later).
    /// </summary>
    internal static class RotorCheck
    {
        private static float _next;
        private static string _message;

        public static void Update()
        {
            if (!TFNet.IsServer || Time.unscaledTime < _next)
                return;
            _next = Time.unscaledTime + 5f;
            var ships = GameServices.ShipsServer;
            if (ships == null)
                return;
            var holders = new Dictionary<uint, List<(uint Ship, ushort Part)>>();
            foreach (TrackedShipServer ship in ships.Ships)
            {
                foreach (StatefulPart part in ship.StatefulParts)
                {
                    if (part.State is RotorState rotor && rotor.AnchoredShipId != 0)
                    {
                        if (!holders.TryGetValue(rotor.AnchoredShipId, out var list))
                            holders[rotor.AnchoredShipId] = list = new List<(uint, ushort)>();
                        list.Add((ship.Id, part.Id));
                    }
                }
            }
            var duplicates = holders.Where(h => h.Value.Count > 1).ToList();
            if (duplicates.Count == 0)
            {
                _message = null;
                return;
            }
            var extra = duplicates.SelectMany(d => d.Value.OrderBy(r => r.Ship).Skip(1)).ToList();
            string ships2 = string.Join(", ", extra.Select(r => "n°" + r.Ship).Distinct());
            string text = $"Rotor en double (copie de schématique) : recycle le rotor du vaisseau {ships2}, pas celui du vaisseau d'origine.";
            if (text != _message)
                TFMod.Log.Warning($"{duplicates.Count} turning part(s) held by several rotors; extra rotors on ship(s) {ships2}");
            _message = text;
        }

        public static void Draw()
        {
            if (_message == null)
                return;
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 2.5f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var rect = new Rect((Screen.width / scale - 640f) / 2f, 60f, 640f, 44f);
            GUI.Box(rect, GUIContent.none, UiTextures.RoundedStyle(new Color(0.35f, 0.08f, 0.05f, 0.9f), 10));
            GUI.Label(new Rect(rect.x + 14f, rect.y, rect.width - 28f, rect.height), _message, DeviceUi.Text);
            GUI.matrix = previous;
        }
    }
}
