using System;
using System.Linq;
using GameCore.HotUpdate.ReduxUI;
using UnityEngine;

namespace BetterCrafting
{
    // The fill after a click on a recipe. The game fills the recipe from the active left tab when it can. When
    // the other linked places (the bag, the drawer, the Tool Cabinets) cover the rest, it moves what the left
    // tab has and marks the tabs that have the rest. Else it moves nothing and shows "not enough materials".
    // The Prefix decides before the game runs whether the mod will fill, so that the text can be skipped.
    // The Postfix reads the grid after the game and moves what is missing.
    internal static class AnyStorageOnRecipeClick
    {
        public static void Prefix(Ac_ToolTable_RecipeClick ac, State_Web_ToolTable state, State_Data_Item itemState)
        {
            AnyStorageFill.ModFills = false;
            try
            {
                var needed = AnyStorageFill.Needed(ac, state);
                if (needed == null) return;
                var scan = Sources.Scan(state, itemState);
                var missing = AnyStorageLogic.Missing(needed, scan.Grid.Select(i => (i.ConfigId, i.Count)));
                bool gameCovers = missing.Count == 0 || AnyStorageLogic.Pick(missing, scan.Candidates.Where(c => c.Tier <= 1)).Count > 0;
                AnyStorageFill.ModFills = !gameCovers && AnyStorageLogic.Pick(missing, scan.Candidates).Count > 0;
                AnyStorageFill.ModFillsFrame = Time.frameCount;
                Plugin.Debug("Click " + AnyStorageLogic.RecipeKey(ac.JsonData) + ": missing " + AnyStorageFill.Counts(missing) + ", mod fills=" + AnyStorageFill.ModFills + ". " + scan.HomeLog);
            }
            catch (Exception e) { Plugin.WarnOnce("Recipe click", e); }
        }

        public static void Postfix(Ac_ToolTable_RecipeClick ac, State_Web_ToolTable state, State_Data_Item itemState)
        {
            bool stillMissing = true;
            try
            {
                var needed = AnyStorageFill.Needed(ac, state);
                if (needed == null) return;
                var scan = Sources.Scan(state, itemState);
                var missing = AnyStorageLogic.Missing(needed, scan.Grid.Select(i => (i.ConfigId, i.Count)));
                if (missing.Count == 0) { stillMissing = false; Plugin.Debug("Fill: nothing missing after the game's fill."); return; }

                if (AnyStorageFill.MoveMissing(needed, missing, scan, state, itemState) == null) return;
                stillMissing = false;
            }
            catch (Exception e) { Plugin.WarnOnce("Fill", e); }
            finally
            {
                // When the Prefix skipped the game's text but the fill did not happen, show the text after all,
                // so the player sees the same result as without the mod.
                bool skippedText = AnyStorageFill.ModFills;
                AnyStorageFill.ModFills = false;
                if (skippedText && stillMissing) AnyStorageFill.ShowShortageText();
            }
        }
    }
}
