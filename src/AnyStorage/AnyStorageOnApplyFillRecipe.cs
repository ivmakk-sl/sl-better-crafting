using System;
using System.Linq;
using GameCore.HotUpdate.ReduxUI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppGeneric = Il2CppSystem.Collections.Generic;

namespace BetterCrafting
{
    // Craft Again fills through ApplyFillRecipe, which looks only at the active left tab and the workbench.
    // When they do not cover the recipe, the mod fills from all its places and gives the game the grid
    // content, so the game checks the grid and starts the craft as after its own fill. A recipe click
    // calls ApplyFillRecipe only when the game's own check found the materials, so it runs unchanged.
    internal static class AnyStorageOnApplyFillRecipe
    {
        public static bool Prefix(State_Web_ToolTable state, State_Data_Item itemState, string recipeKey,
            ref Il2CppGeneric.List<long> filledItemIds, ref bool __result)
        {
            try
            {
                if (state == null || itemState == null || state.HandMadeState != 0) return true;
                if (string.IsNullOrEmpty(recipeKey) || AnyStorageFill.IsLocked(state, recipeKey)) return true;
                var counts = Reducer_Web_ToolTable.CountMaterials(Reducer_Web_ToolTable.ParseMaterialKey(recipeKey));
                if (counts == null) return true;
                var direct = new Il2CppStructArray<long>(new[] { state.BagOwnerId.Value, state.WorkbenchOwnerId.Value });
                if (Reducer_Web_ToolTable.SelectItemsForRecipe(itemState, counts, direct) != null) return true;

                var needed = AnyStorageFill.ToDictionary(counts);
                var scan = Sources.Scan(state, itemState);
                var missing = AnyStorageLogic.Missing(needed, scan.Grid.Select(i => (i.ConfigId, i.Count)));
                if (missing.Count == 0) return true;
                Plugin.Debug("Craft again: " + recipeKey + ", missing " + AnyStorageFill.Counts(missing) + ".");
                var filled = AnyStorageFill.MoveMissing(needed, missing, scan, state, itemState);
                if (filled == null) return true;
                filledItemIds = filled;
                __result = true;
                return false;
            }
            catch (Exception e) { Plugin.WarnOnce("Craft again", e); return true; }
        }
    }
}
