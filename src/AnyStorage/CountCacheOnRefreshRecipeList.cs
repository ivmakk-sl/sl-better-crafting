namespace BetterCrafting
{
    // The span of the count tables: one refresh of the recipe list. The Finalizer also closes it when the
    // refresh throws.
    internal static class CountCacheOnRefreshRecipeList
    {
        public static void Prefix() => CountCache.Open();

        public static void Postfix() => CountCache.Close(true);

        public static void Finalizer() => CountCache.Close(false);
    }
}
