using System;
using System.Linq;
using BepInEx.Unity.IL2CPP;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using Il2CppGeneric = Il2CppSystem.Collections.Generic;

namespace BetterCrafting
{
    // Craft from any storage: the workbench fill takes the missing materials from all home storage, and the
    // recipe list counts them. The feature is patched all or nothing, because a part of it alone misleads the
    // player (a recipe list that counts home storage with no fill from it).
    internal static class AnyStorageFeature
    {
        // A game method and the patch methods of the class Patches that attach to it.
        private sealed class PatchTarget
        {
            public Type Type;
            public string Method;
            public Type Patches;
            public string Prefix;
            public string Postfix;
            public Type[] Args;
        }

        private static readonly PatchTarget[] Targets =
        {
            new PatchTarget { Type = typeof(Reducer_Web_ToolTable), Method = "RA_RecipeClick", Patches = typeof(AnyStorageOnRecipeClick), Prefix = nameof(AnyStorageOnRecipeClick.Prefix), Postfix = nameof(AnyStorageOnRecipeClick.Postfix) },
            new PatchTarget { Type = typeof(Ac_PopText_AddPopText), Method = "SendAction", Patches = typeof(AnyStorageOnPopText), Prefix = nameof(AnyStorageOnPopText.Prefix) },
            new PatchTarget { Type = typeof(Reducer_Web_ToolTable), Method = "RefreshRecipeList", Patches = typeof(AnyStorageOnRefreshRecipeList), Postfix = nameof(AnyStorageOnRefreshRecipeList.Postfix) },
            new PatchTarget
            {
                Type = typeof(Reducer_Web_ToolTable), Method = "ApplyFillRecipe", Patches = typeof(AnyStorageOnApplyFillRecipe), Prefix = nameof(AnyStorageOnApplyFillRecipe.Prefix),
                Args = new[] { typeof(State_Web_ToolTable), typeof(State_Data_Item), typeof(string), typeof(Il2CppGeneric.List<long>).MakeByRefType() },
            },
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
            var found = Targets.Select(t => (target: t, method: AccessTools.Method(t.Type, t.Method, t.Args))).ToList();
            var missing = found.Where(f => f.method == null).Select(f => f.target.Type.Name + "." + f.target.Method).ToList();
            if (missing.Count > 0)
            {
                Plugin.Log.LogWarning("Game methods not found: " + string.Join(", ", missing) + ". Craft from any storage is off.");
                return;
            }
            foreach (var (target, method) in found)
                harmony.Patch(method, prefix: Hook(target.Patches, target.Prefix), postfix: Hook(target.Patches, target.Postfix));
            Plugin.Debug("Craft from any storage: on");
        }

        private static HarmonyMethod Hook(Type patches, string name) =>
            name == null ? null : new HarmonyMethod(AccessTools.Method(patches, name));
    }
}
