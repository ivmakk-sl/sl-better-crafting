using System;
using System.Linq;
using BepInEx.Unity.IL2CPP;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

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
            // The argument types, for a method with an overload.
            public Type[] Args;
            public Type Patches;
            public string Prefix;
            public string Postfix;
            public string Finalizer;
        }

        private static readonly PatchTarget[] Targets =
        {
            new PatchTarget { Type = typeof(Reducer_Web_ToolTable), Method = "GetAllLinkedOwnerIds", Patches = typeof(AnyStorageOnGetAllLinkedOwnerIds), Postfix = nameof(AnyStorageOnGetAllLinkedOwnerIds.Postfix) },
            // Game 1.0 also has GetAllLinkedOwnerIds, but its fill does not use it. This 1.1 method of the fill
            // turns the feature off on 1.0.
            new PatchTarget { Type = typeof(Reducer_Web_ToolTable), Method = "GetLinkedOwnersInFillOrder" },
        };

        // The count cache: one count of the places for each refresh of the recipe list. A game update that
        // renames one of these turns off only the cache, and the game counts as in 1.1.0. The interop has two
        // CountOwnedInPool: the game method with an Il2CppStructArray, and a helper with a long[] that calls it.
        private static readonly PatchTarget[] CacheTargets =
        {
            new PatchTarget { Type = typeof(Reducer_Web_ToolTable), Method = "RefreshRecipeList", Patches = typeof(CountCacheOnRefreshRecipeList),
                Prefix = nameof(CountCacheOnRefreshRecipeList.Prefix), Postfix = nameof(CountCacheOnRefreshRecipeList.Postfix), Finalizer = nameof(CountCacheOnRefreshRecipeList.Finalizer) },
            new PatchTarget { Type = typeof(Reducer_Web_ToolTable), Method = "CountOwnedInPool", Args = new[] { typeof(State_Data_Item), typeof(int), typeof(Il2CppStructArray<long>) },
                Patches = typeof(CountCacheOnCountOwnedInPool), Prefix = nameof(CountCacheOnCountOwnedInPool.Prefix) },
            new PatchTarget { Type = typeof(Reducer_Web_ToolTable), Method = "CheckMaterialSufficiencyAcrossLinked", Patches = typeof(CountCacheOnCheckMaterialSufficiency),
                Prefix = nameof(CountCacheOnCheckMaterialSufficiency.Prefix) },
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
            if (!PatchAll(harmony, Targets, "Craft from any storage")) return;
            Plugin.Debug("Craft from any storage: on");
            try
            {
                if (PatchAll(harmony, CacheTargets, "The count cache")) Plugin.Debug("Count cache: patched, CountCache=" + Plugin.CountCache.Value);
            }
            catch (Exception e) { Plugin.WarnOnce("Count cache patch", e, "The game counts the materials itself."); }
        }

        // Patches all targets of a group, or none when a target is missing.
        private static bool PatchAll(Harmony harmony, PatchTarget[] targets, string name)
        {
            var found = targets.Select(t => (target: t, method: AccessTools.Method(t.Type, t.Method, t.Args))).ToList();
            var missing = found.Where(f => f.method == null).Select(f => f.target.Type.Name + "." + f.target.Method).ToList();
            if (missing.Count > 0)
            {
                Plugin.Log.LogWarning("Game methods not found: " + string.Join(", ", missing) + ". " + name + " is off.");
                return false;
            }
            foreach (var (target, method) in found.Where(f => f.target.Patches != null))
                harmony.Patch(method, Hook(target, target.Prefix), Hook(target, target.Postfix), finalizer: Hook(target, target.Finalizer));
            return true;
        }

        private static HarmonyMethod Hook(PatchTarget target, string name) =>
            name == null ? null : new HarmonyMethod(AccessTools.Method(target.Patches, name));
    }
}
