using System.Collections.Generic;

namespace BetterCrafting
{
    // The owner list of the game's fill with home storage, with no game or BepInEx type, so the xUnit project
    // tests it.
    internal static class AnyStorageLogic
    {
        // The game's owners in their order, then each home owner that is not 0, not excluded, and not in the
        // list yet, in its order.
        public static List<long> AppendOwners(IReadOnlyList<long> gameOwners, IEnumerable<long> homeOwners, ISet<long> excluded)
        {
            var owners = new List<long>(gameOwners);
            var seen = new HashSet<long>(gameOwners);
            foreach (long owner in homeOwners)
                if (owner != 0 && !excluded.Contains(owner) && seen.Add(owner)) owners.Add(owner);
            return owners;
        }
    }
}
