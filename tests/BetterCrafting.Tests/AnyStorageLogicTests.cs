using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BetterCrafting.Tests
{
    public class AnyStorageLogicTests
    {
        private const int Cardboard = 20101;
        private const int WoodenPlank = 20106;
        private const int Wire = 20104;

        private static Dictionary<int, int> Needed(params (int configId, int count)[] materials)
        {
            var needed = new Dictionary<int, int>();
            foreach (var (configId, count) in materials) needed[configId] = count;
            return needed;
        }

        [Fact]
        public void MaterialFullyOnGridIsNotMissing()
        {
            var missing = AnyStorageLogic.Missing(Needed((Cardboard, 2)), new[] { (Cardboard, 1), (Cardboard, 1) });
            Assert.Empty(missing);
        }

        [Fact]
        public void MaterialPartlyOnGridMissesTheRest()
        {
            var missing = AnyStorageLogic.Missing(Needed((WoodenPlank, 4)), new[] { (WoodenPlank, 1) });
            Assert.Equal(new Dictionary<int, int> { [WoodenPlank] = 3 }, missing);
        }

        [Fact]
        public void GridItemThatTheRecipeDoesNotNeedIsIgnored()
        {
            var missing = AnyStorageLogic.Missing(Needed((Wire, 1)), new[] { (WoodenPlank, 5) });
            Assert.Equal(new Dictionary<int, int> { [Wire] = 1 }, missing);
        }

        [Fact]
        public void EmptyGridMissesEachMaterial()
        {
            var missing = AnyStorageLogic.Missing(Needed((Wire, 1), (WoodenPlank, 2)), new (int, int)[0]);
            Assert.Equal(new Dictionary<int, int> { [Wire] = 1, [WoodenPlank] = 2 }, missing);
        }

        private const long Bag = 1, Home = 2, OtherHome = 3, Drawer = 4;
        private const int NoShelfLife = -24;

        private static AnyStorageLogic.Candidate Item(long id, int configId, long owner = Home, int tier = 2, int count = 1, int timeLeft = NoShelfLife, bool polluted = false) =>
            new AnyStorageLogic.Candidate { ItemId = id, Owner = owner, Tier = tier, ConfigId = configId, Count = count, TimeLeft = timeLeft, Polluted = polluted };

        [Fact]
        public void BagBeforeHomeStorage()
        {
            var pick = AnyStorageLogic.Pick(Needed((Cardboard, 1)), new[] { Item(10, Cardboard), Item(11, Cardboard, Bag, tier: 0) });
            Assert.Equal(new long[] { 11 }, pick);
        }

        [Fact]
        public void BagThenDrawerThenHomeStorage()
        {
            var pick = AnyStorageLogic.Pick(Needed((Wire, 3)),
                new[] { Item(10, Wire), Item(11, Wire, Drawer, tier: 1), Item(12, Wire, Bag, tier: 0), Item(13, Wire, OtherHome) });
            Assert.Equal(new long[] { 12, 11, 10 }, pick);
        }

        [Fact]
        public void PollutedInTheBagBeforeCleanInTheDrawer()
        {
            var pick = AnyStorageLogic.Pick(Needed((Wire, 1)), new[] { Item(10, Wire, Drawer, tier: 1), Item(11, Wire, Bag, tier: 0, polluted: true) });
            Assert.Equal(new long[] { 11 }, pick);
        }

        [Fact]
        public void CleanBeforePolluted()
        {
            var pick = AnyStorageLogic.Pick(Needed((WoodenPlank, 1)), new[] { Item(10, WoodenPlank, polluted: true), Item(11, WoodenPlank) });
            Assert.Equal(new long[] { 11 }, pick);
        }

        [Fact]
        public void TwoDaysBeforeSixDays()
        {
            var pick = AnyStorageLogic.Pick(Needed((WoodenPlank, 1)), new[] { Item(10, WoodenPlank, timeLeft: 6 * 24), Item(11, WoodenPlank, timeLeft: 2 * 24) });
            Assert.Equal(new long[] { 11 }, pick);
        }

        [Fact]
        public void ShelfLifeBeforeNoShelfLife()
        {
            var pick = AnyStorageLogic.Pick(Needed((WoodenPlank, 1)), new[] { Item(10, WoodenPlank, timeLeft: 0), Item(11, WoodenPlank, timeLeft: 48) });
            Assert.Equal(new long[] { 11 }, pick);
        }

        [Fact]
        public void PollutedOnlyWhenCleanIsNotEnough()
        {
            var pick = AnyStorageLogic.Pick(Needed((WoodenPlank, 2)), new[] { Item(10, WoodenPlank, polluted: true), Item(11, WoodenPlank), Item(12, WoodenPlank, polluted: true) });
            Assert.Equal(2, pick.Count);
            Assert.Contains(11L, pick);
        }

        [Fact]
        public void WireFromBagAndPlanksFromHomeStorage()
        {
            var pick = AnyStorageLogic.Pick(Needed((Wire, 1), (WoodenPlank, 2)),
                new[] { Item(10, Wire, Bag, tier: 0), Item(11, WoodenPlank), Item(12, WoodenPlank, OtherHome) });
            Assert.Equal(new long[] { 10, 11, 12 }, pick.OrderBy(i => i));
        }

        [Fact]
        public void MaterialMissingEverywhereGivesNoPick()
        {
            var pick = AnyStorageLogic.Pick(Needed((Wire, 3), (WoodenPlank, 2)), new[] { Item(11, WoodenPlank, count: 5) });
            Assert.Empty(pick);
        }

        [Fact]
        public void StackLargerThanMissingIsOneItem()
        {
            var pick = AnyStorageLogic.Pick(Needed((WoodenPlank, 2)), new[] { Item(10, WoodenPlank, count: 40), Item(11, WoodenPlank, count: 5) });
            Assert.Equal(new long[] { 10 }, pick);
        }

        [Fact]
        public void SeveralSmallStacksCoverTheCount()
        {
            var pick = AnyStorageLogic.Pick(Needed((WoodenPlank, 4)), new[] { Item(10, WoodenPlank), Item(11, WoodenPlank, count: 2), Item(12, WoodenPlank, count: 2) });
            Assert.Equal(new long[] { 11, 12 }, pick);
        }

        [Fact]
        public void OtherMaterialsAreNotPicked()
        {
            var pick = AnyStorageLogic.Pick(Needed((Wire, 1)), new[] { Item(10, WoodenPlank), Item(11, Wire) });
            Assert.Equal(new long[] { 11 }, pick);
        }

        private static string Material(string name, int need, int have, string sub = "null") =>
            "{\"icon\":\"i/" + name + "\",\"name\":\"" + name + "\",\"need\":" + need + ",\"have\":" + have + ",\"hasEnough\":" + (have >= need ? "true" : "false") + ",\"sub\":" + sub + "}";

        private static Dictionary<string, int> Haves(params (string name, int have)[] haves) =>
            haves.ToDictionary(h => h.name, h => h.have);

        [Fact]
        public void EachCountGoesToTheMaterialOfItsName()
        {
            // The game lists Lockpick's materials in the recipe's order, not by item id.
            string json = "[" + Material("Waste Plastic", 1, 0) + "," + Material("Broken Glass", 1, 8) + "]";
            var haves = new Dictionary<string, int> { ["Broken Glass"] = 141, ["Waste Plastic"] = 93 };
            string expected = "[" + Material("Waste Plastic", 1, 93) + "," + Material("Broken Glass", 1, 141) + "]";
            Assert.Equal(expected, AnyStorageLogic.RecountMaterialsJson(json, haves));
        }

        [Fact]
        public void MaterialWithoutACountKeepsTheGameValues()
        {
            string json = "[" + Material("Wire", 1, 0) + "," + Material("Container", 1, 3) + "]";
            string expected = "[" + Material("Wire", 1, 4) + "," + Material("Container", 1, 3) + "]";
            Assert.Equal(expected, AnyStorageLogic.RecountMaterialsJson(json, Haves(("Wire", 4))));
        }

        [Fact]
        public void EscapedNameIsFound()
        {
            string json = "[{\"name\":\"Rope \\\"A\\\" \\u00e9\",\"need\":1,\"have\":0,\"hasEnough\":false}]";
            string expected = "[{\"name\":\"Rope \\\"A\\\" \\u00e9\",\"need\":1,\"have\":2,\"hasEnough\":true}]";
            Assert.Equal(expected, AnyStorageLogic.RecountMaterialsJson(json, Haves(("Rope \"A\" \u00e9", 2))));
        }

        [Fact]
        public void RecountOneMaterial()
        {
            string json = "[" + Material("Cardboard", 2, 1) + "]";
            Assert.Equal("[" + Material("Cardboard", 2, 6) + "]", AnyStorageLogic.RecountMaterialsJson(json, Haves(("Cardboard", 6))));
        }

        [Fact]
        public void RecountTwoMaterials()
        {
            string json = "[" + Material("Wire", 1, 1) + "," + Material("Wooden Plank", 2, 0) + "]";
            string expected = "[" + Material("Wire", 1, 1) + "," + Material("Wooden Plank", 2, 5) + "]";
            Assert.Equal(expected, AnyStorageLogic.RecountMaterialsJson(json, Haves(("Wire", 1), ("Wooden Plank", 5))));
        }

        [Fact]
        public void SubObjectIsKept()
        {
            string sub = "{\"name\":\"Plank {x}\",\"have\":3,\"list\":[{\"need\":9}]}";
            string json = "[" + Material("Wooden Plank", 2, 0, sub) + "]";
            Assert.Equal("[" + Material("Wooden Plank", 2, 4, sub) + "]", AnyStorageLogic.RecountMaterialsJson(json, Haves(("Wooden Plank", 4))));
        }

        [Fact]
        public void CountEqualToNeedIsEnough()
        {
            Assert.Contains("\"hasEnough\":true", AnyStorageLogic.RecountMaterialsJson("[" + Material("Wire", 3, 0) + "]", Haves(("Wire", 3))));
        }

        [Fact]
        public void CountBelowNeedIsNotEnough()
        {
            Assert.Contains("\"hasEnough\":false", AnyStorageLogic.RecountMaterialsJson("[" + Material("Wire", 3, 3) + "]", Haves(("Wire", 2))));
        }

        [Fact]
        public void PicksFromTheLeftTabGiveOneMove()
        {
            var steps = AnyStorageLogic.MoveSteps(new long[] { 1 }, new[] { (11L, Bag), (12L, Bag) }, Bag);
            Assert.Single(steps);
            Assert.Equal(Bag, steps[0].LeftOwnerId);
            Assert.Equal(new long[] { 1, 11, 12 }, steps[0].Target);
        }

        [Fact]
        public void PickFromHomeStorageIsASecondMoveFromThatStorage()
        {
            var steps = AnyStorageLogic.MoveSteps(new long[] { 1 }, new[] { (11L, Bag), (21L, Home) }, Bag);
            Assert.Equal(2, steps.Count);
            Assert.Equal(Bag, steps[0].LeftOwnerId);
            Assert.Equal(new long[] { 1, 11 }, steps[0].Target);
            Assert.Equal(Home, steps[1].LeftOwnerId);
            Assert.Equal(new long[] { 1, 11, 21 }, steps[1].Target);
        }

        [Fact]
        public void NoPickFromTheLeftTabStillMovesTheGridFirst()
        {
            var steps = AnyStorageLogic.MoveSteps(new long[] { 1 }, new[] { (21L, Home) }, Bag);
            Assert.Equal(2, steps.Count);
            Assert.Equal(Bag, steps[0].LeftOwnerId);
            Assert.Equal(new long[] { 1 }, steps[0].Target);
            Assert.Equal(Home, steps[1].LeftOwnerId);
            Assert.Equal(new long[] { 1, 21 }, steps[1].Target);
        }

        [Fact]
        public void TwoHomeStorageMoveInTheOrderOfTheirFirstPick()
        {
            var steps = AnyStorageLogic.MoveSteps(new long[0], new[] { (31L, OtherHome), (21L, Home) }, Bag);
            Assert.Equal(new[] { Bag, OtherHome, Home }, steps.Select(s => s.LeftOwnerId));
            Assert.Equal(new long[] { 31, 21 }, steps[2].Target);
        }

        [Fact]
        public void PicksOfOneOwnerMoveInOneCall()
        {
            var steps = AnyStorageLogic.MoveSteps(new long[0], new[] { (21L, Home), (31L, OtherHome), (22L, Home) }, Bag);
            Assert.Equal(new[] { Bag, Home, OtherHome }, steps.Select(s => s.LeftOwnerId));
            Assert.Equal(new long[] { 21, 22 }, steps[1].Target);
            Assert.Equal(new long[] { 21, 22, 31 }, steps[2].Target);
        }

        [Fact]
        public void RecipeKeyFromClickJson()
        {
            Assert.Equal("20101,20101", AnyStorageLogic.RecipeKey("{\"recipeKey\":\"20101,20101\",\"recipeId\":0}"));
        }

        [Fact]
        public void NoRecipeKeyGivesNull()
        {
            Assert.Null(AnyStorageLogic.RecipeKey("{\"recipeId\":3}"));
            Assert.Null(AnyStorageLogic.RecipeKey(null));
        }
    }
}
