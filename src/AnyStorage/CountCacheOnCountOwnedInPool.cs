using System;
using GameCore.HotUpdate.ReduxUI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace BetterCrafting
{
    // The game's count of one material in the places. Inside a refresh of the recipe list, the count comes from
    // the table of the refresh and the game's body is skipped. Else, and after an error, the game counts.
    internal static class CountCacheOnCountOwnedInPool
    {
        public static bool Prefix(State_Data_Item itemState, int matConfigId, Il2CppStructArray<long> ownerIds, ref int __result)
        {
            if (!CountCache.Active || itemState?.OwnerCache == null || ownerIds == null) return true;
            try
            {
                __result = CountCache.Count(itemState, ownerIds, matConfigId);
                return false;
            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Count cache", e, "The game counts the materials itself.");
                return true;
            }
        }
    }
}
