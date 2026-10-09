using System;
using System.Linq;
using FishNet.Connection;
using Items.Model;
using StellarDriveDemoTF.Common;
using Tools.PaintTool;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StellarDriveDemoTF.Paint
{
    /// <summary>
    /// The demo ships the paint tool but never hands it out (the default toolbar only holds the build
    /// tool, recycler and mining laser). A key press asks the host for one.
    /// </summary>
    internal static class PaintToolGiver
    {
        private static string _keyName;
        private static Key _key = Key.None;

        public static void Update()
        {
            Key key = ConfiguredKey();
            if (key == Key.None || Keyboard.current == null || !Keyboard.current[key].wasPressedThisFrame)
                return;
            if (GameServices.ShipsClient == null)
                return; // not in a world
            PaintNet.RequestPaintTool();
        }

        public static void ServerGive(NetworkConnection sender)
        {
            if (sender == null)
                return;
            var players = GameServices.PlayersServer;
            var player = players?.GetTrackedPlayerFromConnectionId(sender.ClientId);
            if (player == null)
                return;

            ItemSettings paintTool = FindPaintToolItem();
            if (paintTool == null)
            {
                TFMod.Log.Warning("no paint tool item found in this game version");
                return;
            }

            var drop = new ItemDrop { ItemId = paintTool.id, Quantity = 1 };
            if (player.HasItemsInInventory(new[] { drop }))
                return;
            player.AddItemToInventory(drop);
            TFMod.Log.Msg($"gave the paint tool (item {paintTool.id}) to {player.Name}");
        }

        public static ItemSettings FindPaintToolItem()
        {
            return Resources.FindObjectsOfTypeAll<ItemSettings>().FirstOrDefault(item =>
                item != null && item.toolSettings != null && item.toolSettings.prefab != null &&
                item.toolSettings.prefab.GetComponentInChildren<PaintTool>(true) != null);
        }

        private static Key ConfiguredKey()
        {
            string name = Settings.GivePaintToolKey.Value;
            if (name == _keyName)
                return _key;

            _keyName = name;
            _key = Key.None;
            if (!string.IsNullOrWhiteSpace(name) && !Enum.TryParse(name.Trim(), true, out _key))
            {
                TFMod.Log.Warning($"unknown key '{name}' for GivePaintToolKey");
                _key = Key.None;
            }
            return _key;
        }
    }
}
