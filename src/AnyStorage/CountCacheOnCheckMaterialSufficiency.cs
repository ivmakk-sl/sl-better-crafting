using System;
using System.Collections.Generic;
using GameCore.HotUpdate.ReduxUI;
using Il2CppGeneric = Il2CppSystem.Collections.Generic;

namespace BetterCrafting
{
    // The game's check of enough materials for one recipe of the list: its pick of SelectItemsForRecipe over the
    // places and the workbench grid. That pick reads, groups, and sorts all stacks once for each recipe. Inside a
    // refresh of the recipe list, the answer comes from the table of the refresh with the same pick rule, and the
    // game's body is skipped. Else, and after an error, the game checks.
    internal static class CountCacheOnCheckMaterialSufficiency
    {
        public static bool Prefix(State_Web_ToolTable state, State_Data_Item itemState, Il2CppGeneric.Dictionary<int, int> materialNeeded, ref bool __result)
        {
            if (!CountCache.Active || state?.WorkbenchOwnerId == null || itemState?.OwnerCache == null || materialNeeded == null) return true;
            try
            {
                var need = new List<KeyValuePair<int, int>>(materialNeeded.Count);
                var en = materialNeeded.GetEnumerator();
                while (en.MoveNext()) need.Add(new KeyValuePair<int, int>(en.Current.Key, en.Current.Value));

                __result = CountCache.CanPick(state, itemState, need);
                return false;
            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Count cache check", e, "The game checks the materials itself.");
                return true;
            }
        }
    }
}
