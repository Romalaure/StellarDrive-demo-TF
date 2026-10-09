using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Services;
using Managers.Client.Utils;
using MelonLoader.Utils;
using Newtonsoft.Json;
using Players;
using Players.Services;
using Saving.Services.Converters;
using Saving.Services.Model.State;
using Ships;
using Ships.Cables;
using Ships.Interface.Model.Joints;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.Common;
using Ships.Interface.Model.Parts.State;
using Ships.Interface.Model.State;
using Ships.Services;
using StellarDriveDemoTF.Common;
using StellarDriveDemoTF.Paint;
using UnityEngine;
using WorldTracking.Interface.Model.Spaces;
using WorldTracking.Interface.Model.Spaces.Common;
using WorldTracking.Services;
using WorldTracking.Spaces;

namespace StellarDriveDemoTF.Devices
{
    /// <summary>
    /// Ship schematics, opened from the schematic tablet item: copy the nearest ship (with everything docked to it, its cables, TF paint
    /// and capsule names) to a file in UserData/TF/schematics, and build it again later, in this
    /// world or another, in front of you. Built on the game's own (developer) ship export and
    /// import. Only the host can use them, since building creates ships in the world it runs.
    /// Building is free: no resources are taken.
    /// </summary>
    internal static class Schematics
    {
        private const int FormatVersion = 1;

        private static string Folder => Path.Combine(MelonEnvironment.UserDataDirectory, "TF", "schematics");

        private sealed class SchematicFile
        {
            public int version = FormatVersion;
            public string name;
            public string created;
            public int ships;
            public int parts;
            public string game;
            public List<SavedPaint> paint = new List<SavedPaint>();
            public List<SavedName> capsules = new List<SavedName>();
        }

        private sealed class SavedPaint
        {
            public uint ship;
            public ushort part;
            public string color;
            public bool finish;
            public byte gloss, metal, glow;
        }

        private sealed class SavedName
        {
            public uint ship;
            public ushort part;
            public string name;
        }

        public sealed class Entry
        {
            public string Path;
            public string Name;
            public string Created;
            public int Ships;
            public int Parts;
        }

        public static List<Entry> List()
        {
            var result = new List<Entry>();
            if (!Directory.Exists(Folder))
                return result;
            foreach (string path in Directory.GetFiles(Folder, "*.json"))
            {
                try
                {
                    // Only the header is needed for the list
                    var file = JsonConvert.DeserializeObject<SchematicFile>(File.ReadAllText(path));
                    if (file == null)
                        continue;
                    result.Add(new Entry { Path = path, Name = file.name ?? System.IO.Path.GetFileNameWithoutExtension(path), Created = file.created, Ships = file.ships, Parts = file.parts });
                }
                catch (Exception e)
                {
                    TFMod.Log.Warning($"schematic {path} unreadable: {e.Message}");
                }
            }
            return result.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static void Delete(Entry entry)
        {
            try
            {
                File.Delete(entry.Path);
            }
            catch (Exception e)
            {
                TFMod.Log.Warning("could not delete schematic: " + e.Message);
            }
        }

        /// <summary>The ship the copy would take: the root of the ship nearest to the player.</summary>
        public static TrackedShipServer NearestShip()
        {
            TrackedPlayerLocalClient player = ServiceLocator.GetService<PlayersClientTracker>()?.LocalPlayer;
            ShipsServerTracker ships = GameServices.ShipsServer;
            if (player == null || ships == null)
                return null;
            TrackedShipClient nearest = NearestShipUtils.GetNearestShipClient(player.SpaceId, player.Position);
            return nearest != null && ships.TryGetTrackedRootShip(nearest.Id, out TrackedShipServer root) ? root : null;
        }

        /// <summary>Saves the nearest ship and everything docked to it. Returns a message for the player.</summary>
        public static string Copy(string name)
        {
            if (!TFNet.IsServer)
                return "Seul l'hôte de la partie peut copier un vaisseau.";
            name = TeleportCapsule.CleanName(name);
            if (name.Length == 0)
                return "Donne un nom à la schématique.";
            ShipsServerTracker service = GameServices.ShipsServer;
            SpacesServer spaces = ServiceLocator.GetService<SpacesServer>();
            TrackedShipServer ship = NearestShip();
            if (service == null || spaces == null || ship == null)
                return "Aucun vaisseau à proximité.";

            // The game's export: snapshots of the root ship and the ships attached to it, with
            // attached ships placed relative to their parent
            var data = new ShipImportExportUtils.ShipImportExportData { rootShipId = ship.Id };
            var snapshots = new Dictionary<uint, ShipStateSnapshot>();
            foreach (RootRelativeShipData connected in service.GetRootConnectedShips(ship.Id))
            {
                var snapshot = (ShipStateSnapshot)((TrackedShipServer)connected.Ship).GetStateSnapshot();
                snapshots[snapshot.Id] = snapshot;
            }
            foreach (uint id in snapshots.Keys.ToList())
            {
                ShipStateSnapshot snapshot = snapshots[id];
                if (snapshot.Id == data.rootShipId)
                {
                    snapshot.SpaceId = 0u;
                }
                else
                {
                    if (!spaces.TryGetSpace(snapshot.SpaceId, out ISpace space) || !(space is ShipRelativeSpace relative))
                        return "Ce vaisseau ne peut pas être copié (vaisseau attaché introuvable).";
                    snapshot.SpaceId = relative.ShipId;
                }
                snapshots[id] = snapshot;
            }
            data.ships = new ShipsConverter().ConvertToSerializable(new SavedShipStates(snapshots.Values.Cast<IShipState>().ToArray()));
            CablesServerTracker cablesTracker = ServiceLocator.GetService<CablesServerTracker>();
            var cables = new Dictionary<uint, CableData>();
            if (cablesTracker != null)
            {
                var shipIds = new HashSet<uint>(snapshots.Keys);
                foreach (uint shipId in shipIds)
                {
                    foreach (CableData cable in cablesTracker.GetShipCables(shipId))
                    {
                        var a = cable.State.PlugA;
                        var b = cable.State.PlugB;
                        if (!a.IsHeldByPlayer && shipIds.Contains(a.EntityId) && !b.IsHeldByPlayer && shipIds.Contains(b.EntityId))
                            cables[cable.Id] = cable;
                    }
                }
            }
            data.cables = CablesConverter.ConvertToSerializable(cables.Values.ToArray());

            var file = new SchematicFile
            {
                name = name,
                created = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                ships = snapshots.Count,
                parts = snapshots.Values.Sum(s => s.StatefulParts.Count() + s.HullParts.Count()),
                game = JsonUtility.ToJson(data)
            };
            foreach (KeyValuePair<PartKey, PaintData> paint in PaintNet.Server.All)
            {
                if (!snapshots.ContainsKey(paint.Key.ShipId))
                    continue;
                PaintData value = paint.Value;
                file.paint.Add(new SavedPaint
                {
                    ship = paint.Key.ShipId,
                    part = paint.Key.PartId,
                    color = $"{value.Color.r:X2}{value.Color.g:X2}{value.Color.b:X2}",
                    finish = value.Finish.Enabled,
                    gloss = value.Finish.Gloss,
                    metal = value.Finish.Metal,
                    glow = value.Finish.Glow
                });
            }
            foreach (KeyValuePair<PartKey, string> capsule in TeleportCapsule.ServerNames)
            {
                if (snapshots.ContainsKey(capsule.Key.ShipId))
                    file.capsules.Add(new SavedName { ship = capsule.Key.ShipId, part = capsule.Key.PartId, name = capsule.Value });
            }

            Directory.CreateDirectory(Folder);
            string path = System.IO.Path.Combine(Folder, FileNameFor(name));
            File.WriteAllText(path, JsonConvert.SerializeObject(file, Formatting.Indented));
            TFMod.Log.Msg($"schematic '{name}' saved: {file.ships} ship(s), {file.parts} part(s) -> {path}");
            return $"« {name} » enregistrée : {file.ships} vaisseau(x), {file.parts} pièces.";
        }

        /// <summary>Builds a saved schematic in front of the player. Returns a message for the player.</summary>
        public static string Build(Entry entry)
        {
            if (!TFNet.IsServer)
                return "Seul l'hôte de la partie peut construire une schématique.";
            SchematicFile file;
            ShipImportExportUtils.ShipImportExportData data;
            try
            {
                file = JsonConvert.DeserializeObject<SchematicFile>(File.ReadAllText(entry.Path));
                data = JsonUtility.FromJson<ShipImportExportUtils.ShipImportExportData>(file.game);
            }
            catch (Exception e)
            {
                return "Fichier illisible : " + e.Message;
            }
            if (data?.ships == null || data.ships.Length == 0)
                return "Cette schématique est vide.";

            var ids = ServiceLocator.GetService<WorldObjectsIdsServerTracker>();
            ShipsServerTracker ships = GameServices.ShipsServer;
            SpacesServer spaces = ServiceLocator.GetService<SpacesServer>();
            CablesServerTracker cablesTracker = ServiceLocator.GetService<CablesServerTracker>();
            TrackedPlayerLocalClient player = ServiceLocator.GetService<PlayersClientTracker>()?.LocalPlayer;
            if (ids == null || ships == null || spaces == null || player == null)
                return "Le monde n'est pas prêt.";

            // Where to build: in the player's world space (outside any ship), ahead of them
            if (!TryWorldPlacement(player, out uint spaceId, out Vector3 position, out Quaternion rotation))
                return "Impossible de trouver où construire.";

            CableData[] cables = CablesConverter.ConvertToState(data.cables ?? new Saving.Serialization.V12.Cables.SerializableCable[0]);
            SavedShipState[] states = new ShipsConverter().ConvertToState(data.ships).States.Cast<SavedShipState>().ToArray();
            uint rootId = data.rootShipId;
            var newIds = new Dictionary<uint, uint>();
            var oldIds = new Dictionary<uint, uint>();
            foreach (SavedShipState state in states)
            {
                state.Velocity = Vector3.zero;
                uint fresh = ids.CreateUniqueIdForObject();
                newIds[state.Id] = fresh;
                oldIds[fresh] = state.Id;
                state.Id = fresh;
            }
            for (int i = 0; i < cables.Length; i++)
            {
                CableData cable = cables[i];
                CableState cableState = cable.State;
                CablePlugSocket plugA = cableState.PlugA;
                EntityPartSocket socketA = plugA.EntityPartSocket;
                socketA.EntityId = newIds[socketA.EntityId];
                plugA.EntityPartSocket = socketA;
                cableState.PlugA = plugA;
                CablePlugSocket plugB = cableState.PlugB;
                EntityPartSocket socketB = plugB.EntityPartSocket;
                socketB.EntityId = newIds[socketB.EntityId];
                plugB.EntityPartSocket = socketB;
                cableState.PlugB = plugB;
                cable.State = cableState;
                cables[i] = cable;
            }
            float extent = 4f;
            foreach (SavedShipState state in states)
            {
                if (oldIds[state.Id] != rootId)
                    state.SpaceId = newIds[state.SpaceId];
                foreach (StatefulPart part in state.StatefulParts)
                {
                    if (part.State is RotorState rotor && newIds.TryGetValue(rotor.AnchoredShipId, out uint anchored))
                    {
                        part.State = rotor with { AnchoredShipId = anchored };
                    }
                    else if (part.State is DockingDoorState door && newIds.TryGetValue(door.DockedConnectedPart.ShipId, out uint docked))
                    {
                        PartContext connected = door.DockedConnectedPart;
                        connected.ShipId = docked;
                        part.State = door with { DockedConnectedPart = connected };
                    }
                }
                if (oldIds[state.Id] == rootId)
                {
                    foreach (HullPart hull in state.HullParts)
                        extent = Mathf.Max(extent, hull.HullPlacement.Coord.magnitude);
                }
            }
            foreach (SavedShipState state in states)
            {
                if (oldIds[state.Id] != rootId)
                    continue;
                state.SpaceId = spaceId;
                // Ahead of the player, far enough for the ship not to land on them, a little up
                Vector3 forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, rotation * Vector3.up).normalized;
                state.Position = position + forward * (extent + 5f) + rotation * Vector3.up * 2f;
                state.Rotation = rotation;
            }
            var relativeSpaces = new Dictionary<uint, uint>();
            foreach (SavedShipState state in states)
            {
                uint relative = spaces.GetOrCreateShipRelativeSpace(state.Id);
                relativeSpaces[state.Id] = relative;
                state.RelativeSpaceId = relative;
            }
            foreach (SavedShipState state in states)
            {
                if (oldIds[state.Id] != rootId)
                    state.SpaceId = relativeSpaces[state.SpaceId];
            }
            foreach (SavedShipState state in states)
                ships.LoadShip(state);
            if (cablesTracker != null)
            {
                foreach (CableData cable in cables)
                    cablesTracker.AddCable(cable.State);
            }

            // TF extras follow the ships to their new ids
            foreach (SavedPaint paint in file.paint ?? new List<SavedPaint>())
            {
                if (!newIds.TryGetValue(paint.ship, out uint ship) || !Outfits.TryParse(paint.color, out Color color))
                    continue;
                var finish = paint.finish ? new PaintFinish { Enabled = true, Gloss = paint.gloss, Metal = paint.metal, Glow = paint.glow } : PaintFinish.None;
                PaintNet.ServerSet(new PartKey(ship, paint.part), new PaintData(PaintNet.ToColor32(color), finish));
            }
            foreach (SavedName capsule in file.capsules ?? new List<SavedName>())
            {
                if (newIds.TryGetValue(capsule.ship, out uint ship))
                    TFNet.SendToServer(TFMessageKind.CapsuleName, ship, capsule.part, capsule.name);
            }
            TFMod.Log.Msg($"schematic '{file.name}' built: {states.Length} ship(s)");
            return $"« {file.name} » construite devant toi.";
        }

        // Climbs out of ship spaces to the space the outermost ship flies in
        private static bool TryWorldPlacement(TrackedPlayerLocalClient player, out uint spaceId, out Vector3 position, out Quaternion rotation)
        {
            spaceId = player.SpaceId;
            position = player.Position;
            rotation = player.Rotation;
            SpacesServer spaces = ServiceLocator.GetService<SpacesServer>();
            ShipsServerTracker ships = GameServices.ShipsServer;
            for (int depth = 0; depth < 8; depth++)
            {
                if (!spaces.TryGetSpace(spaceId, out ISpace space))
                    return false;
                if (!(space is ShipRelativeSpace relative))
                    return true;
                if (!ships.TryGetTrackedShip(relative.ShipId, out TrackedShipServer ship))
                    return false;
                position = ship.Position + ship.Rotation * position;
                rotation = ship.Rotation * rotation;
                spaceId = ship.SpaceId;
            }
            return false;
        }

        private static string FileNameFor(string name)
        {
            var clean = new string(name.Select(c => System.IO.Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
            return clean + ".json";
        }
    }

    /// <summary>The schematics window.</summary>
    internal static class SchematicsMenu
    {
        private static bool _open;
        private static string _name = "";
        private static string _message;
        private static Vector2 _scroll;
        private static List<Schematics.Entry> _entries = new List<Schematics.Entry>();

        public static void Show()
        {
            _open = true;
            _message = null;
            _entries = Schematics.List();
            ModMenu.Open(() => _open = false);
        }

        public static void Draw()
        {
            if (!_open || !ModMenu.IsOpen)
                return;
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 2.5f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale, height = Screen.height / scale;

            Rect area = DeviceUi.Window(width, height, 620f, 680f, "SCHÉMATIQUES DE VAISSEAUX");
            GUILayout.BeginArea(area);
            if (!TFNet.IsServer)
            {
                GUILayout.Label("Seul l'hôte de la partie peut copier et construire des vaisseaux : les schématiques sont enregistrées sur son PC.", DeviceUi.Text);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Fermer (Échap)", DeviceUi.Button, GUILayout.Height(34f)))
                    ModMenu.Close();
                GUILayout.EndArea();
                GUI.matrix = previous;
                return;
            }

            TrackedShipServer nearest = Schematics.NearestShip();
            GUILayout.Label("COPIER LE VAISSEAU LE PLUS PROCHE", DeviceUi.Muted);
            GUILayout.Label(nearest != null
                ? $"Vaisseau n°{nearest.Id} : {nearest.StatefulParts.Count()} pièces, {nearest.GetHullParts().Count} éléments de coque (+ les vaisseaux amarrés)"
                : "Aucun vaisseau à proximité.", DeviceUi.Text);
            GUILayout.BeginHorizontal();
            _name = GUILayout.TextField(_name ?? "", TeleportCapsule.MaxNameLength, DeviceUi.Field, GUILayout.Height(34f), GUILayout.ExpandWidth(true));
            GUI.enabled = nearest != null;
            if (GUILayout.Button("Copier", DeviceUi.Button, GUILayout.Width(100f), GUILayout.Height(34f)))
            {
                _message = Run(() => Schematics.Copy(_name));
                _entries = Schematics.List();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(14f);

            GUILayout.Label("SCHÉMATIQUES ENREGISTRÉES (tous les mondes)", DeviceUi.Muted);
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(360f));
            if (_entries.Count == 0)
                GUILayout.Label("Aucune pour l'instant : copie un vaisseau ci-dessus.", DeviceUi.Muted);
            foreach (Schematics.Entry entry in _entries.ToList())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{entry.Name}\n{entry.Ships} vaisseau(x) · {entry.Parts} pièces · {entry.Created}", DeviceUi.Text, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Construire", DeviceUi.Button, GUILayout.Width(110f), GUILayout.Height(40f)))
                    _message = Run(() => Schematics.Build(entry));
                if (GUILayout.Button("Suppr.", DeviceUi.Button, GUILayout.Width(70f), GUILayout.Height(40f)))
                {
                    Schematics.Delete(entry);
                    _entries = Schematics.List();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4f);
            }
            GUILayout.EndScrollView();
            if (_message != null)
                GUILayout.Label(_message, DeviceUi.Text);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Construire est gratuit et place le vaisseau devant toi. Les schématiques sont dans UserData/TF/schematics.", DeviceUi.Muted);
            if (GUILayout.Button("Fermer (Échap)", DeviceUi.Button, GUILayout.Height(34f)))
                ModMenu.Close();
            GUILayout.EndArea();
            GUI.matrix = previous;
        }

        private static string Run(Func<string> action)
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                TFMod.Log.Error("schematic: " + e);
                return "Erreur : " + e.Message;
            }
        }
    }
}
