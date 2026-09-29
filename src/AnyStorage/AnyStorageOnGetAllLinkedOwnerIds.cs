using System;
using System.Collections.Generic;
using GameCore.HotUpdate.ReduxUI;
using Il2CppGeneric = Il2CppSystem.Collections.Generic;

namespace BetterCrafting
{
    // The game's list of the places of the workbench window: the bag, the workbench drawer, and each Tool
    // Cabinet tab. The game's fill, Craft Again, the dye choice, each craft of a batch craft, the recipe list
    // counts, and the maximum of a batch craft all read it. The Postfix appends home storage after the game's
    // owners, so the game's own code uses home storage too. The game builds a new list on each call.
    internal static class AnyStorageOnGetAllLinkedOwnerIds
    {
        public static void Postfix(State_Web_ToolTable state, Il2CppGeneric.List<long> __result)
        {
            try
            {
                if (state == null || __result == null)
                {
                    Plugin.Debug("Home storage: no window state or no list from the game");
                    return;
                }
                var gameOwners = new List<long>(__result.Count);
                for (int i = 0; i < __result.Count; i++) gameOwners.Add(__result[i]);
                var excluded = new HashSet<long> { 0, state.WorkbenchOwnerId?.Value ?? 0, state.CurrentFurnitureId };
                var skip = new HashSet<long>(excluded);
                skip.UnionWith(gameOwners);
                var owners = AnyStorageLogic.AppendOwners(gameOwners, Sources.HomeOwners(state, skip), excluded);
                for (int i = gameOwners.Count; i < owners.Count; i++) __result.Add(owners[i]);
            }
            catch (Exception e) { Plugin.WarnOnce("Home storage list", e); }
        }
    }
}
