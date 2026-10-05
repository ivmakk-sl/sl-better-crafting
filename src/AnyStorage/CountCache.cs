using System;
using System.Collections.Generic;
using System.Text;
using GameCore.HotUpdate.ReduxUI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace BetterCrafting
{
    // The count tables of one refresh of the recipe list. The game counts each material of each recipe again
    // over all stacks of the places, and with home storage in the places that is the cost of a craft. A refresh
    // runs in one call on the main thread, so the bag mirror does not change inside it, and the tables are
    // valid until it ends. A count call outside a refresh (a click on a recipe) or in another frame (a refresh
    // that ended without a Finalizer) runs the game's own count.
    internal static class CountCache
    {
        private static bool open;
        private static int frame = -1;
        // The items of each owner, read once for each refresh.
        private static readonly Dictionary<long, List<CountTable.Item>> ownerItems = new Dictionary<long, List<CountTable.Item>>();
        // A table for each owner list in its order: the callers of the game build their own lists.
        private static readonly TableSet tables = new TableSet();
        // The owner ids of a count call, copied into one buffer for all calls.
        private static long[] buffer = new long[64];
        // The owner list of the sufficiency check, built once for each refresh of the same window state.
        private static IntPtr checkState;
        private static long[] checkOwners;
        private static readonly List<string> built = new List<string>();
        // Set when ItemsOf repaired a ghost of an owner.
        private static bool healed;
        private static int countsServed;
        private static int checksServed;

        public static bool Active => open && frame == Time.frameCount && Plugin.CountCache.Value;

        public static void Open()
        {
            Reset();
            open = true;
            frame = Time.frameCount;
        }

        public static void Close(bool log)
        {
            if (log && open && Plugin.Verbose.Value && (tables.Count > 0 || countsServed > 0 || checksServed > 0))
                Plugin.Debug("Count cache: frame=" + frame + " tables=" + tables.Count + " (" + string.Join("; ", built)
                             + ") counts served=" + countsServed + " sufficiency checks served=" + checksServed);
            Reset();
        }

        private static void Reset()
        {
            open = false;
            ownerItems.Clear();
            tables.Clear();
            checkState = IntPtr.Zero;
            checkOwners = null;
            built.Clear();
            countsServed = 0;
            checksServed = 0;
        }

        public static int Count(State_Data_Item itemState, Il2CppStructArray<long> ownerIds, int configId)
        {
            countsServed++;
            int n = ownerIds.Length;
            if (buffer.Length < n) buffer = new long[n * 2];
            for (int i = 0; i < n; i++) buffer[i] = ownerIds[i];
            return TableFor(itemState, buffer, n).Count(configId);
        }

        public static bool CanPick(State_Web_ToolTable state, State_Data_Item itemState, List<KeyValuePair<int, int>> materialNeeded)
        {
            checksServed++;
            var owners = CheckOwners(state);
            return TableFor(itemState, owners, owners.Length).CanPick(materialNeeded);
        }

        // The game's owner list of the sufficiency check: the places of GetAllLinkedOwnerIds (with home storage),
        // then the workbench. It is the same for each recipe of one refresh.
        private static long[] CheckOwners(State_Web_ToolTable state)
        {
            if (checkOwners != null && checkState == state.Pointer) return checkOwners;
            var linked = Reducer_Web_ToolTable.GetAllLinkedOwnerIds(state);
            var ids = new long[linked.Count + 1];
            for (int i = 0; i < linked.Count; i++) ids[i] = linked[i];
            ids[linked.Count] = state.WorkbenchOwnerId.Value;
            checkState = state.Pointer;
            checkOwners = ids;
            return ids;
        }

        private static CountTable TableFor(State_Data_Item itemState, long[] ownerIds, int count)
        {
            var table = tables.Find(ownerIds, count);
            if (table != null) return table;

            var ids = new long[count];
            Array.Copy(ownerIds, ids, count);
            healed = false;
            var items = new List<CountTable.Item>();
            foreach (long owner in ids) items.AddRange(ItemsOf(itemState, owner));
            if (healed)
            {
                // A repair moves a ghost into the cache entry of its real owner, which can be an owner read
                // before it, here or for an earlier table. Read all owners again after the repair.
                ownerItems.Clear();
                tables.Clear();
                items.Clear();
                foreach (long owner in ids) items.AddRange(ItemsOf(itemState, owner));
            }
            table = CountTable.Build(items);
            tables.Add(ids, table);
            if (Plugin.Verbose.Value) built.Add("owners " + ids.Length + " stacks " + items.Count);
            return table;
        }

        // The items of one owner in the bag mirror, the data that the game's GetItemDataList reads. When the
        // owner has a ghost item, the game's GetItemDataList repairs the cache first, as the game's count does
        // on its first read of that owner.
        private static List<CountTable.Item> ItemsOf(State_Data_Item itemState, long owner)
        {
            if (ownerItems.TryGetValue(owner, out var list)) return list;
            list = Read(itemState, owner, out bool ghost);
            if (ghost)
            {
                State_Extensions_Item.GetItemDataList(itemState, owner);
                healed = true;
                list = Read(itemState, owner, out _);
            }
            ownerItems[owner] = list;
            return list;
        }

        private static List<CountTable.Item> Read(State_Data_Item itemState, long owner, out bool ghost)
        {
            ghost = false;
            var list = new List<CountTable.Item>();
            var cache = itemState.OwnerCache;
            if (owner == 0 || !cache.ContainsKey(owner)) return list;
            var en = cache[owner].GetEnumerator();
            while (en.MoveNext())
            {
                var item = en.Current.Value;
                if (item == null) continue;
                long itemOwner = item.OwnerId;
                if (itemOwner != owner) ghost = true;
                list.Add(new CountTable.Item(owner, itemOwner, item.ItemConfigId, item.ItemCount));
            }
            return list;
        }
    }
}
