using System;

namespace BetterCrafting
{
    // The span of the count tables: one refresh of the recipe list. The Finalizer also closes it when the
    // refresh throws; it returns void, so the game's own exception still goes on. An error of the cache closes
    // the cache, and the game counts by itself.
    internal static class CountCacheOnRefreshRecipeList
    {
        public static void Prefix() => Guard(() => CountCache.Open());

        public static void Postfix() => Guard(() => CountCache.Close(true));

        public static void Finalizer() => Guard(() => CountCache.Close(false));

        private static void Guard(Action action)
        {
            try { action(); }
            catch (Exception e)
            {
                Plugin.WarnOnce("Count cache refresh", e, "The game counts the materials itself.");
                try { CountCache.Close(false); } catch { }
            }
        }
    }
}
