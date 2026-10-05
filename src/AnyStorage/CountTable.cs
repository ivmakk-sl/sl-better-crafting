using System.Collections.Generic;

namespace BetterCrafting
{
    // The material counts of the places of one recipe list refresh, built in one pass over their items, with no
    // game or BepInEx type, so the xUnit project tests it. It gives the answers of the game's CountOwnedInPool
    // with the same rules, so the game reads the places once for each refresh in place of once for each material
    // of each recipe.
    internal sealed class CountTable
    {
        // One item of the bag mirror: the owner id of its cache entry, its own owner id, its config id, and its count.
        public readonly struct Item
        {
            public readonly long CacheOwner;
            public readonly long ItemOwner;
            public readonly int ConfigId;
            public readonly int Count;

            public Item(long cacheOwner, long itemOwner, int configId, int count)
            {
                CacheOwner = cacheOwner;
                ItemOwner = itemOwner;
                ConfigId = configId;
                Count = count;
            }
        }

        private readonly Dictionary<int, int> counts = new Dictionary<int, int>();
        // The raw count of each stack of a config id, sorted largest first at the first CanPick.
        private readonly Dictionary<int, List<int>> stacks = new Dictionary<int, List<int>>();
        private bool sorted;
        private int items;

        public static CountTable Build(IEnumerable<Item> items)
        {
            var table = new CountTable();
            foreach (var item in items)
            {
                // The game skips the owner id 0, and GetItemDataList skips an item whose own owner id is not
                // the owner of its cache entry (a ghost).
                if (item.CacheOwner == 0 || item.ItemOwner != item.CacheOwner) continue;
                table.counts.TryGetValue(item.ConfigId, out int n);
                // The game counts a stack with a count below 1 as one item.
                table.counts[item.ConfigId] = n + (item.Count < 1 ? 1 : item.Count);
                if (!table.stacks.TryGetValue(item.ConfigId, out var list)) table.stacks[item.ConfigId] = list = new List<int>();
                list.Add(item.Count);
                table.items++;
            }
            return table;
        }

        public int Count(int configId) => counts.TryGetValue(configId, out int n) ? n : 0;

        // The answer of the game's SelectItemsForRecipe over the places: whether its pick finds the materials.
        // It walks the stacks of each material largest first and takes a stack when its count (below 1 as 1) is
        // at most the rest of the need. A material with no stack fails, also with a need of 0, and so do places
        // with no item at all.
        public bool CanPick(IEnumerable<KeyValuePair<int, int>> materialNeeded)
        {
            if (items == 0) return false;
            if (!sorted)
            {
                foreach (var list in stacks.Values) list.Sort((a, b) => b.CompareTo(a));
                sorted = true;
            }
            foreach (var need in materialNeeded)
            {
                if (!stacks.TryGetValue(need.Key, out var list)) return false;
                int rest = need.Value;
                foreach (int raw in list)
                {
                    if (rest < 1) break;
                    int count = raw < 1 ? 1 : raw;
                    if (count <= rest) rest -= count;
                }
                if (rest >= 1) return false;
            }
            return true;
        }
    }

    // The tables of one refresh, one for each owner list in its order: the callers of the game build their own
    // lists (with or without the workbench owner). A refresh has one or two lists, so a compare of the owner ids
    // finds a table with no key text for each of the hundreds of count calls.
    internal sealed class TableSet
    {
        private readonly List<(long[] ids, CountTable table)> entries = new List<(long[], CountTable)>();

        public int Count => entries.Count;

        // The table of the first count ids of the buffer, or null.
        public CountTable Find(long[] buffer, int count)
        {
            foreach (var (ids, table) in entries)
            {
                if (ids.Length != count) continue;
                int i = 0;
                while (i < count && ids[i] == buffer[i]) i++;
                if (i == count) return table;
            }
            return null;
        }

        public void Add(long[] ids, CountTable table) => entries.Add((ids, table));

        public void Clear() => entries.Clear();
    }
}
