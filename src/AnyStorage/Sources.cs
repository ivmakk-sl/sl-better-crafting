using System.Collections.Generic;
using System.Text;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace BetterCrafting
{
    // The home storage that the game's fill can use: each furniture that passes the game's home scope rule,
    // the seed rule of the planting window, and has items. The workbench, the furniture of the open window,
    // and the game's own owners are left out. The game asks for its owner list once for each recipe of a list
    // refresh, so the list is kept for the current frame and the open window, and the furniture is scanned
    // again only when one of them changes.
    internal static class Sources
    {
        private static List<long> owners = new List<long>();
        private static int frame = -1;
        private static long furniture;
        private static int calls;

        public static List<long> HomeOwners(State_Web_ToolTable state, ICollection<long> skip)
        {
            int now = Time.frameCount;
            if (now == frame && state.CurrentFurnitureId == furniture)
            {
                calls++;
                return owners;
            }
            int previousCalls = calls;
            frame = now;
            furniture = state.CurrentFurnitureId;
            calls = 1;
            owners = Scan(skip, previousCalls);
            return owners;
        }

        private static List<long> Scan(ICollection<long> skip, int previousCalls)
        {
            var result = new List<long>();
            // The store keeps the data states (State_Data_*) apart from the window states: GetState gives null
            // for State_Data_Item, GetData gives it.
            var itemState = ReduxUISystem.Instance?.GetData<State_Data_Item>(Il2CppType.Of<State_Data_Item>());
            var cache = itemState?.OwnerCache;
            if (cache == null)
            {
                Plugin.Debug("Home storage: frame=" + frame + " no item state (state " + (itemState == null ? "null" : "found") + ")");
                return result;
            }

            var world = BaseSingleton<BattleLogicWorld>.Instance;
            var agentManager = world._AgentManager;
            int homeMapId = agentManager.GetHomeMapId();
            int homeGroup = ConfigManager.Instance.Get_Config_MapPoint(homeMapId)?.HomeGroup ?? 0;
            var included = Plugin.Verbose.Value ? new StringBuilder() : null;
            var skipped = Plugin.Verbose.Value ? new StringBuilder() : null;
            int skippedOutside = 0;
            var furnitures = agentManager.GetAllFurnitures();
            for (int i = 0; furnitures != null && i < furnitures.Count; i++)
            {
                var f = furnitures[i];
                if (f == null || skip.Contains(f.InstanceId) || !cache.ContainsKey(f.InstanceId)) continue;
                int items = cache[f.InstanceId].Count;
                if (items == 0) continue;
                if (!ItemManager.IsInHomeScope(f, homeMapId, homeGroup))
                {
                    // The furniture list holds the whole world, so only the skipped storage of the home is
                    // listed: a locked floor or area of the home shows here.
                    if (skipped == null) continue;
                    if (f.MapConfigId == homeMapId || (homeGroup > 0 && ConfigManager.Instance.Get_Config_MapPoint(f.MapConfigId)?.HomeGroup == homeGroup))
                        skipped.Append(" skipped ").Append(f.InstanceId).Append(" map ").Append(f.MapConfigId)
                               .Append(FloorTagConstants.IsMapPointUnlocked(world._TagManager, f.MapConfigId) ? " unlocked" : " LOCKED")
                               .Append(" items ").Append(items);
                    else skippedOutside++;
                    continue;
                }
                result.Add(f.InstanceId);
                included?.Append(' ').Append(f.InstanceId).Append(" map ").Append(f.MapConfigId).Append(" items ").Append(items);
            }
            if (included != null)
                Plugin.Debug("Home storage: frame=" + frame + " previous frame calls=" + previousCalls + " owners=" + result.Count
                             + included + skipped + " skipped outside the home: " + skippedOutside);
            return result;
        }
    }
}
