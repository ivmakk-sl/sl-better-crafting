using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
using GameCore.HotUpdate.ReduxUI;

namespace BetterCrafting
{
    // The recipe list counts the same places as the fill: the "have" count of each material and the mark of
    // a recipe with enough materials (CanCraft). The game counts only its own sources (CountOwnedInPool over
    // GetAllLinkedOwnerIds). A recipe that the game locks keeps the game's values.
    internal static class AnyStorageOnRefreshRecipeList
    {
        public static void Postfix(State_Web_ToolTable state, State_Data_Item itemState)
        {
            try
            {
                var list = state?.RecipeList;
                if (list == null || list.Count == 0) return;
                var scan = Sources.Scan(state, itemState);
                var total = new Dictionary<int, int>();
                foreach (var c in scan.Grid.Concat(scan.Candidates))
                    total[c.ConfigId] = (total.TryGetValue(c.ConfigId, out int n) ? n : 0) + c.Count;

                // The item names in the display language, as the game writes them into MaterialsJson. The
                // language can change while the game runs, so they are read again on each refresh.
                var names = new Dictionary<int, string>();
                int changed = 0;
                string firstBefore = null, firstAfter = null;
                bool firstHasMore = false;
                for (int i = 0; i < list.Count; i++)
                {
                    var recipe = list[i];
                    if (recipe == null || recipe.CraftLocked.Value) continue;
                    string key = recipe.RecipeKey.Value;
                    if (string.IsNullOrEmpty(key)) continue;
                    var counts = Reducer_Web_ToolTable.CountMaterials(Reducer_Web_ToolTable.ParseMaterialKey(key));
                    if (counts == null) continue;

                    // A name that two materials of the recipe share is left out, so both keep the game's values.
                    var haveByName = new Dictionary<string, int>();
                    var shared = new HashSet<string>();
                    bool enough = true;
                    var e = counts.GetEnumerator();
                    while (e.MoveNext())
                    {
                        int have = total.TryGetValue(e.Current.Key, out int n) ? n : 0;
                        if (have < e.Current.Value) enough = false;
                        string name = ItemName(names, e.Current.Key);
                        if (string.IsNullOrEmpty(name)) continue;
                        if (haveByName.ContainsKey(name)) shared.Add(name);
                        haveByName[name] = have;
                    }
                    foreach (string name in shared) haveByName.Remove(name);

                    string before = recipe.MaterialsJson.Value;
                    if (string.IsNullOrEmpty(before)) continue;
                    string after = AnyStorageLogic.RecountMaterialsJson(before, haveByName);
                    // The game sets Partial (the mark of a fill from the other tabs) only when CanCraft is false.
                    bool partial = recipe.Partial.Value && !enough;
                    if (after == before && recipe.CanCraft.Value == enough && recipe.Partial.Value == partial) continue;
                    recipe.MaterialsJson.Value = after;
                    recipe.CanCraft.Value = enough;
                    recipe.Partial.Value = partial;
                    changed++;
                    if (firstBefore == null || (!firstHasMore && counts.Count > 1))
                    {
                        firstBefore = before;
                        firstAfter = after;
                        firstHasMore = counts.Count > 1;
                    }
                }
                Plugin.Debug("Recipe list: " + changed + " recipes recounted." + (firstBefore != null ? " First: " + firstBefore + " -> " + firstAfter : ""));
            }
            catch (Exception e) { Plugin.WarnOnce("Recipe list", e); }
        }

        private static string ItemName(Dictionary<int, string> names, int configId)
        {
            if (names.TryGetValue(configId, out string name)) return name;
            var item = ConfigManager.Instance.Get_Config_Item(configId);
            name = item == null ? null : ConstantTextTools.GetLocalText(item.ItemName);
            names[configId] = name;
            return name;
        }
    }
}
