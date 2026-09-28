using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
using GameCore.HotUpdate.ReduxUI;
using Il2CppGeneric = Il2CppSystem.Collections.Generic;

namespace BetterCrafting
{
    // The fill that the recipe click and Craft Again share. SyncWorkbenchTo sets the full content of the grid
    // and moves the items in the same call, but it takes each new item from its leftOwnerId, so the fill
    // calls it once for each owner of the picked items.
    internal static class AnyStorageFill
    {
        public const string ShortageTextKey = "SR_Web_ToolTable_1";

        // Set by the click Prefix when the mod will fill, so the game's "not enough materials" text is skipped.
        public static bool ModFills;
        public static int ModFillsFrame = -1;

        // Moves the picked items of the missing materials to the grid, with one SyncWorkbenchTo call for each
        // owner. Returns the grid content as the game got it in the last call, or null when the fill places
        // cannot cover each missing material.
        public static Il2CppGeneric.List<long> MoveMissing(Dictionary<int, int> needed, Dictionary<int, int> missing,
            Sources.Result scan, State_Web_ToolTable state, State_Data_Item itemState)
        {
            var pick = AnyStorageLogic.Pick(missing, scan.Candidates);
            if (pick.Count == 0) { Plugin.Debug("Fill: not enough in the home for " + Counts(missing) + "."); return null; }

            var byId = scan.Candidates.ToDictionary(c => c.ItemId);
            Plugin.Debug("Fill: " + string.Join(", ", pick.Select(id => Describe(byId[id]))));
            var gridIds = scan.Grid.Where(i => needed.ContainsKey(i.ConfigId)).Select(i => i.ItemId).ToList();
            var steps = AnyStorageLogic.MoveSteps(gridIds, pick.Select(id => (id, byId[id].Owner)), Reducer_Web_ToolTable.GetActiveLeftOwnerId(state));
            int before = gridIds.Count;
            Il2CppGeneric.List<long> target = null;
            foreach (var step in steps)
            {
                // SyncWorkbenchTo can swap an id in this list for a grid item of the same kind and count.
                target = new Il2CppGeneric.List<long>();
                foreach (long id in step.Target) target.Add(id);
                Plugin.Debug("Fill: move " + (step.Target.Count - before) + " items from " + step.LeftOwnerId);
                before = step.Target.Count;
                Reducer_Web_ToolTable.SyncWorkbenchTo(state, itemState, target, step.LeftOwnerId);
            }
            Reducer_Web_ToolTable.ClearCabinetShortage(state);
            Ac_ToolTable_RefreshBag.SendAction();
            return target;
        }

        public static void ShowShortageText()
        {
            try { Ac_PopText_AddPopText.SendAction(ConstantTextTools.ToConstantTextOrEmpty(ShortageTextKey)); }
            catch (Exception e) { Plugin.WarnOnce("Shortage text", e); }
        }

        // The materials of the clicked recipe, or null when the game does not fill it: the game shows the
        // lock text of a locked recipe and returns before its fill, so the mod does not fill it either.
        public static Dictionary<int, int> Needed(Ac_ToolTable_RecipeClick ac, State_Web_ToolTable state)
        {
            string key = AnyStorageLogic.RecipeKey(ac?.JsonData);
            if (string.IsNullOrEmpty(key) || IsLocked(state, key)) return null;
            var counts = Reducer_Web_ToolTable.CountMaterials(Reducer_Web_ToolTable.ParseMaterialKey(key));
            return counts == null ? null : ToDictionary(counts);
        }

        public static Dictionary<int, int> ToDictionary(Il2CppGeneric.Dictionary<int, int> counts)
        {
            var needed = new Dictionary<int, int>();
            var e = counts.GetEnumerator();
            while (e.MoveNext()) needed[e.Current.Key] = e.Current.Value;
            return needed;
        }

        public static bool IsLocked(State_Web_ToolTable state, string key)
        {
            var list = state?.RecipeList;
            for (int i = 0; list != null && i < list.Count; i++)
            {
                var recipe = list[i];
                if (recipe != null && recipe.RecipeKey.Value == key) return recipe.CraftLocked.Value;
            }
            return false;
        }

        public static string Counts(Dictionary<int, int> counts) =>
            counts.Count == 0 ? "none" : string.Join(",", counts.Select(p => p.Key + "x" + p.Value));

        private static string Describe(AnyStorageLogic.Candidate c) =>
            c.ItemId + " (cfg " + c.ConfigId + " x" + c.Count + ", owner " + c.Owner + ", tier " + c.Tier + ", time left " + c.TimeLeft + (c.Polluted ? ", polluted" : "") + ")";
    }
}
