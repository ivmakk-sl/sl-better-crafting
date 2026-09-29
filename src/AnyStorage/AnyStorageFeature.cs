using System;
using System.Linq;
using BepInEx.Unity.IL2CPP;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;

namespace BetterCrafting
{
    // Craft from any storage: home storage is a place of the game's own workbench fill, after the game's tabs,
    // so the fill, Craft Again, the dye choice, a batch craft, and the recipe list counts use it. The feature is
    // patched all or nothing, because a part of it alone misleads the player (a recipe list that counts home
    // storage with no fill from it).
    internal static class AnyStorageFeature
    {
        // A game method and the patch methods of the class Patches that attach to it. A target with no
        // Patches is required but not patched.
        private sealed class PatchTarget
        {
            public Type Type;
            public string Method;
            public Type Patches;
            public string Postfix;
        }

        private static readonly PatchTarget[] Targets =
        {
            new PatchTarget { Type = typeof(Reducer_Web_ToolTable), Method = "GetAllLinkedOwnerIds", Patches = typeof(AnyStorageOnGetAllLinkedOwnerIds), Postfix = nameof(AnyStorageOnGetAllLinkedOwnerIds.Postfix) },
            // Game 1.0 also has GetAllLinkedOwnerIds, but its fill does not use it. This 1.1 method of the fill
            // turns the feature off on 1.0.
            new PatchTarget { Type = typeof(Reducer_Web_ToolTable), Method = "GetLinkedOwnersInFillOrder" },
        };

        // BaseButler also changes the workbench fill, and with both mods its page script can start a craft by
        // itself after this fill. The soft dependency of Plugin loads it first, so its entry is in the plugin list here.
        public const string BaseButlerGuid = "com.basebutler.mod";

        // A game update can rename a target. Then the feature patches nothing, so the fill stays the game's own.
        public static void Patch(Harmony harmony)
        {
            if (IL2CPPChainloader.Instance.Plugins.ContainsKey(BaseButlerGuid))
            {
                Plugin.Log.LogWarning("BaseButler is installed. It also changes the workbench fill, so Craft from any storage is off.");
                return;
            }
            var found = Targets.Select(t => (target: t, method: AccessTools.Method(t.Type, t.Method))).ToList();
            var missing = found.Where(f => f.method == null).Select(f => f.target.Type.Name + "." + f.target.Method).ToList();
            if (missing.Count > 0)
            {
                Plugin.Log.LogWarning("Game methods not found: " + string.Join(", ", missing) + ". Craft from any storage is off.");
                return;
            }
            foreach (var (target, method) in found.Where(f => f.target.Patches != null))
                harmony.Patch(method, postfix: new HarmonyMethod(AccessTools.Method(target.Patches, target.Postfix)));
            Plugin.Debug("Craft from any storage: on");
        }
    }
}
