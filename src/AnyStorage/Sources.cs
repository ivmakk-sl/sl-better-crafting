using System.Collections.Generic;
using System.Text;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;

namespace BetterCrafting
{
    // The items that the fill can use: the workbench grid, then in pick order the bag (tier 0), the other
    // sources of the game's own fill, the workbench drawer and the Tool Cabinets (tier 1), and the home
    // storage that passes the game's home scope rule, the seed rule of the planting window (tier 2).
    internal static class Sources
    {
        public sealed class Result
        {
            public List<AnyStorageLogic.Candidate> Grid = new List<AnyStorageLogic.Candidate>();
            public List<AnyStorageLogic.Candidate> Candidates = new List<AnyStorageLogic.Candidate>();
            public string HomeLog = "";
        }

        public static Result Scan(State_Web_ToolTable state, State_Data_Item itemState)
        {
            var result = new Result();
            long workbench = state.WorkbenchOwnerId.Value;
            AddItems(result.Grid, itemState, workbench, 0);

            // BagOwnerId is the active left tab, which can be the drawer or a Tool Cabinet. PlayerBagOwnerId is the bag.
            long bag = state.PlayerBagOwnerId;
            var gameSources = new HashSet<long> { bag, state.BagOwnerId.Value, state.DrawerOwnerId.Value };
            var linked = Reducer_Web_ToolTable.GetAllLinkedOwnerIds(state);
            for (int i = 0; linked != null && i < linked.Count; i++) gameSources.Add(linked[i]);
            gameSources.Remove(workbench);
            gameSources.Remove(0);
            foreach (long owner in gameSources) AddItems(result.Candidates, itemState, owner, owner == bag ? 0 : 1);

            var world = BaseSingleton<BattleLogicWorld>.Instance;
            var agentManager = world._AgentManager;
            int homeMapId = agentManager.GetHomeMapId();
            int homeGroup = ConfigManager.Instance.Get_Config_MapPoint(homeMapId)?.HomeGroup ?? 0;
            var log = Plugin.Verbose.Value ? new StringBuilder("Home storage:") : null;
            int skippedOutside = 0;
            var furnitures = agentManager.GetAllFurnitures();
            for (int i = 0; furnitures != null && i < furnitures.Count; i++)
            {
                var f = furnitures[i];
                if (f == null || f.InstanceId == workbench || f.InstanceId == state.CurrentFurnitureId || gameSources.Contains(f.InstanceId)) continue;
                if (!ItemManager.IsInHomeScope(f, homeMapId, homeGroup))
                {
                    // The furniture list holds the whole world, so only the skipped storage of the home is
                    // listed: a locked floor or area of the home shows here.
                    int items = log != null ? ItemCount(itemState, f.InstanceId) : 0;
                    if (items == 0) continue;
                    if (f.MapConfigId == homeMapId || (homeGroup > 0 && ConfigManager.Instance.Get_Config_MapPoint(f.MapConfigId)?.HomeGroup == homeGroup))
                        log.Append(" skipped ").Append(f.InstanceId).Append(" map ").Append(f.MapConfigId)
                           .Append(FloorTagConstants.IsMapPointUnlocked(world._TagManager, f.MapConfigId) ? " unlocked" : " LOCKED")
                           .Append(" items ").Append(items);
                    else skippedOutside++;
                    continue;
                }
                int before = result.Candidates.Count;
                AddItems(result.Candidates, itemState, f.InstanceId, 2);
                if (log != null && result.Candidates.Count > before)
                    log.Append(' ').Append(f.InstanceId).Append(" map ").Append(f.MapConfigId)
                       .Append(FloorTagConstants.IsMapPointUnlocked(world._TagManager, f.MapConfigId) ? " unlocked" : " LOCKED");
            }
            if (log != null) result.HomeLog = log.Append(" skipped outside the home: ").Append(skippedOutside).ToString();
            return result;
        }

        private static int ItemCount(State_Data_Item itemState, long owner)
        {
            var cache = itemState.OwnerCache;
            return cache != null && cache.ContainsKey(owner) ? cache[owner].Count : 0;
        }

        private static void AddItems(List<AnyStorageLogic.Candidate> list, State_Data_Item itemState, long owner, int tier)
        {
            var cache = itemState.OwnerCache;
            if (cache == null || !cache.ContainsKey(owner)) return;
            var e = cache[owner].GetEnumerator();
            while (e.MoveNext())
            {
                var item = e.Current.Value;
                if (item == null) continue;
                list.Add(new AnyStorageLogic.Candidate
                {
                    ItemId = item.LogicId, Owner = owner, Tier = tier, ConfigId = item.ItemConfigId,
                    Count = item.ItemCount, TimeLeft = item.TimeLeft, Polluted = item.Polluted,
                });
            }
        }
    }
}
