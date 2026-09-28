using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BetterCrafting
{
    // The pick rule of the fill, with no game or BepInEx type, so the xUnit project tests it.
    internal static class AnyStorageLogic
    {
        // The recipe key of the workbench window's click message, for example {"recipeKey":"20101,20101","recipeId":0}.
        public static string RecipeKey(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var m = Regex.Match(json, "\"recipeKey\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        // For each material config id, the needed count less the count on the workbench grid.
        // A material with nothing missing is left out.
        public static Dictionary<int, int> Missing(Dictionary<int, int> needed, IEnumerable<(int configId, int count)> onGrid)
        {
            var have = new Dictionary<int, int>();
            foreach (var (configId, count) in onGrid)
                have[configId] = (have.TryGetValue(configId, out int n) ? n : 0) + count;

            var missing = new Dictionary<int, int>();
            foreach (var need in needed)
            {
                int rest = need.Value - (have.TryGetValue(need.Key, out int n) ? n : 0);
                if (rest > 0) missing[need.Key] = rest;
            }
            return missing;
        }

        // An item that the fill can move to the workbench grid. The pick takes a lower tier first. Tier 0: the
        // bag. Tier 1: the workbench drawer and the Tool Cabinets. Tier 2: the other home storage.
        public sealed class Candidate
        {
            public long ItemId;
            public long Owner;
            public int Tier;
            public int ConfigId;
            public int Count;
            public int TimeLeft;
            public bool Polluted;

            // The game gives a material with no shelf life a TimeLeft of 0 or less.
            public bool HasShelfLife => TimeLeft > 0;
        }

        // The item ids to move so that each missing count is covered, or an empty list when the
        // candidates cannot cover each one. Whole items only, the least valuable first.
        public static List<long> Pick(Dictionary<int, int> missing, IEnumerable<Candidate> candidates)
        {
            var byMaterial = new Dictionary<int, List<Candidate>>();
            foreach (var c in candidates)
            {
                if (!missing.ContainsKey(c.ConfigId) || c.Count <= 0) continue;
                if (!byMaterial.TryGetValue(c.ConfigId, out var list)) byMaterial[c.ConfigId] = list = new List<Candidate>();
                list.Add(c);
            }

            var picked = new List<long>();
            foreach (var need in missing)
            {
                if (!byMaterial.TryGetValue(need.Key, out var list)) return new List<long>();
                list.Sort(LeastValuableFirst);
                int rest = need.Value;
                foreach (var c in list)
                {
                    if (rest <= 0) break;
                    picked.Add(c.ItemId);
                    rest -= c.Count;
                }
                if (rest > 0) return new List<long>();
            }
            return picked;
        }

        // One call of the game's SyncWorkbenchTo: the full content of the workbench grid after the call,
        // and the owner that the new items come from and that the other grid items go to.
        public sealed class MoveStep
        {
            public long LeftOwnerId;
            public List<long> Target;
        }

        // The game's move takes all new grid items from LeftOwnerId, so the fill makes one call for each
        // owner of the picked items. The active left tab comes first, so that the grid items that the
        // recipe does not need go back to it, as in the game's own fill.
        public static List<MoveStep> MoveSteps(IEnumerable<long> gridIds, IEnumerable<(long itemId, long owner)> picks, long leftOwnerId)
        {
            var owners = new List<long> { leftOwnerId };
            var byOwner = new Dictionary<long, List<long>> { [leftOwnerId] = new List<long>() };
            foreach (var (itemId, owner) in picks)
            {
                if (!byOwner.TryGetValue(owner, out var ids))
                {
                    byOwner[owner] = ids = new List<long>();
                    owners.Add(owner);
                }
                ids.Add(itemId);
            }

            var steps = new List<MoveStep>();
            var target = new List<long>(gridIds);
            foreach (long owner in owners)
            {
                target.AddRange(byOwner[owner]);
                steps.Add(new MoveStep { LeftOwnerId = owner, Target = new List<long>(target) });
            }
            return steps;
        }

        // The recipe list's MaterialsJson is a list with one object for each material of the recipe
        // ("icon", "name", "need", "have", "hasEnough", "sub"), in the order of the recipe's material list,
        // which is not the order of the item ids. This sets "have" of each object whose name is in
        // haveByName to that count and "hasEnough" to have >= need, and keeps each other value as the game
        // wrote it. An object whose name is not in haveByName keeps the game's values.
        public static string RecountMaterialsJson(string json, IReadOnlyDictionary<string, int> haveByName)
        {
            var sb = new StringBuilder(json.Length + 16);
            int i = 0;
            while (i < json.Length)
            {
                if (json[i] == '{')
                {
                    int end = ValueEnd(json, i);
                    string obj = json.Substring(i, end - i);
                    string name = Name(obj);
                    sb.Append(name != null && haveByName.TryGetValue(name, out int have) ? Recount(obj, have) : obj);
                    i = end;
                }
                else sb.Append(json[i++]);
            }
            return sb.ToString();
        }

        private static string Recount(string obj, int have)
        {
            var members = Members(obj);
            int need = members.TryGetValue("need", out var n) && int.TryParse(obj.Substring(n.start, n.end - n.start).Trim(), out int v) ? v : 0;
            var replace = new List<(int start, int end, string text)>();
            if (members.TryGetValue("have", out var h)) replace.Add((h.start, h.end, have.ToString()));
            if (members.TryGetValue("hasEnough", out var e)) replace.Add((e.start, e.end, have >= need ? "true" : "false"));
            replace.Sort((a, b) => b.start.CompareTo(a.start));
            foreach (var r in replace) obj = obj.Substring(0, r.start) + r.text + obj.Substring(r.end);
            return obj;
        }

        // The "name" string of one material object, or null when it has none.
        private static string Name(string obj)
        {
            if (!Members(obj).TryGetValue("name", out var n) || obj[n.start] != '"') return null;
            var sb = new StringBuilder();
            for (int i = n.start + 1; i < n.end - 1; i++)
            {
                char c = obj[i];
                if (c != '\\' || i + 1 >= n.end - 1) { sb.Append(c); continue; }
                char e = obj[++i];
                if (e == 'u' && i + 4 < n.end - 1)
                {
                    sb.Append((char)Convert.ToInt32(obj.Substring(i + 1, 4), 16));
                    i += 4;
                }
                else sb.Append(e == 'n' ? '\n' : e == 't' ? '\t' : e == 'r' ? '\r' : e);
            }
            return sb.ToString();
        }

        // The members of one JSON object: each key with the span of its value. Nested values are skipped whole.
        private static Dictionary<string, (int start, int end)> Members(string obj)
        {
            var members = new Dictionary<string, (int start, int end)>();
            int i = 1;
            while (i < obj.Length)
            {
                char c = obj[i];
                if (c == '"')
                {
                    int keyEnd = StringEnd(obj, i);
                    string key = obj.Substring(i + 1, keyEnd - i - 2);
                    int colon = keyEnd;
                    while (colon < obj.Length && char.IsWhiteSpace(obj[colon])) colon++;
                    if (colon >= obj.Length || obj[colon] != ':') { i = keyEnd; continue; }
                    int start = colon + 1;
                    while (start < obj.Length && char.IsWhiteSpace(obj[start])) start++;
                    int end = ValueEnd(obj, start);
                    members[key] = (start, end);
                    i = end;
                }
                else i++;
            }
            return members;
        }

        // The index just after the JSON value that starts at start.
        private static int ValueEnd(string s, int start)
        {
            if (s[start] == '"') return StringEnd(s, start);
            if (s[start] == '{' || s[start] == '[')
            {
                int depth = 0, i = start;
                while (i < s.Length)
                {
                    char c = s[i];
                    if (c == '"') { i = StringEnd(s, i); continue; }
                    if (c == '{' || c == '[') depth++;
                    else if (c == '}' || c == ']') { depth--; if (depth == 0) return i + 1; }
                    i++;
                }
                return s.Length;
            }
            int j = start;
            while (j < s.Length && s[j] != ',' && s[j] != '}' && s[j] != ']') j++;
            return j;
        }

        // The index just after the closing quote of the string that starts at start.
        private static int StringEnd(string s, int start)
        {
            int i = start + 1;
            while (i < s.Length)
            {
                if (s[i] == '\\') { i += 2; continue; }
                if (s[i] == '"') return i + 1;
                i++;
            }
            return s.Length;
        }

        private static int LeastValuableFirst(Candidate a, Candidate b)
        {
            if (a.Tier != b.Tier) return a.Tier.CompareTo(b.Tier);
            if (a.Polluted != b.Polluted) return a.Polluted ? 1 : -1;
            if (a.HasShelfLife != b.HasShelfLife) return a.HasShelfLife ? -1 : 1;
            if (a.HasShelfLife && a.TimeLeft != b.TimeLeft) return a.TimeLeft.CompareTo(b.TimeLeft);
            if (a.Count != b.Count) return b.Count.CompareTo(a.Count);
            return a.ItemId.CompareTo(b.ItemId);
        }
    }
}
