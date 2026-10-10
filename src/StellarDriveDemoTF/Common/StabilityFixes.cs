using System;
using HarmonyLib;
using Managers.Server;

namespace StellarDriveDemoTF.Common
{
    /// <summary>
    /// The game sometimes keeps a planet surface space in its active list after removing it.
    /// From then on, every physics tick of the host throws "Space id N does not exist" while
    /// updating planet colliders, which aborts the rest of that tick: ships, players and the
    /// physics simulation stop updating (players sink into the ground, ships freeze, clients
    /// lose sync). Logs showed thousands of these per session. Skipping the planet collider
    /// update for that tick lets the rest of the physics run.
    /// </summary>
    [HarmonyPatch(typeof(WorldPhysicsServerUpdater), "UpdateLoadedPlanetColliders")]
    internal static class MissingSpaceGuard
    {
        private static int _count;
        private static int _indexCount;

        private static Exception Finalizer(Exception __exception)
        {
            if (__exception is ArgumentException && __exception.Message.Contains("does not exist"))
            {
                if (_count++ % 3000 == 0)
                    TFMod.Log.Warning($"skipped a planet collider update: {__exception.Message} (game bug, {_count} so far)");
                return null;
            }
            // Seen thrown on every tick after loading a world, freezing the player; the rethrow
            // would lose where it came from, so log the full trace here
            if (__exception is IndexOutOfRangeException)
            {
                if (_indexCount++ % 3000 == 0)
                    TFMod.Log.Warning($"skipped a planet collider update ({_indexCount} so far): {__exception}");
                return null;
            }
            return __exception;
        }
    }
}
