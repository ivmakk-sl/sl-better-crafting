using BetterCrafting;
using Xunit;

public class CountTableTests
{
    private static CountTable.Item I(long owner, int configId, int count) => new CountTable.Item(owner, owner, configId, count);

    [Fact]
    public void CountAddsTheStacksOfAConfigIdAndCountsACountBelowOneAsOne()
    {
        var table = CountTable.Build(new[] { I(1, 100, 3), I(1, 200, 5), I(2, 100, 2), I(2, 100, 0), I(2, 100, -1) });

        Assert.Equal(7, table.Count(100));
        Assert.Equal(5, table.Count(200));
        Assert.Equal(0, table.Count(300));
    }

    [Fact]
    public void AGhostItemAndAnOwnerIdOfZeroDoNotCount()
    {
        var table = CountTable.Build(new[]
        {
            I(1, 100, 3),
            new CountTable.Item(1, 9, 100, 50), // a ghost: the item names another owner than its cache entry
            I(0, 100, 20),
        });

        Assert.Equal(3, table.Count(100));
    }

    private static Dictionary<int, int> Need(params (int id, int n)[] m) => m.ToDictionary(x => x.id, x => x.n);

    [Fact]
    public void CanPickTakesAnExactStackAndTwoStacks()
    {
        var table = CountTable.Build(new[] { I(1, 100, 2), I(1, 200, 1), I(2, 200, 1) });

        Assert.True(table.CanPick(Need((100, 2))));
        Assert.True(table.CanPick(Need((200, 2))));
        Assert.True(table.CanPick(Need((100, 2), (200, 2))));
        Assert.False(table.CanPick(Need((200, 3))));
    }

    [Fact]
    public void CanPickSkipsAStackLargerThanTheRestOfTheNeed()
    {
        // The game counts 5 + 1 = 6 here, but its pick takes no stack of 5 for a need of 2.
        var table = CountTable.Build(new[] { I(1, 100, 5), I(1, 100, 1) });

        Assert.Equal(6, table.Count(100));
        Assert.False(table.CanPick(Need((100, 2))));
        Assert.True(table.CanPick(Need((100, 6))));
        Assert.True(table.CanPick(Need((100, 1))));
    }

    [Fact]
    public void CanPickTakesTheLargestStacksFirst()
    {
        // Largest first: 3 is taken for a need of 4, then 2 is too large, then 1 makes 4.
        var table = CountTable.Build(new[] { I(1, 100, 1), I(1, 100, 3), I(1, 100, 2) });

        Assert.True(table.CanPick(Need((100, 4))));
        Assert.True(table.CanPick(Need((100, 5))));
        Assert.True(table.CanPick(Need((100, 6))));
    }

    [Fact]
    public void CanPickCountsAStackWithACountBelowOneAsOne()
    {
        var table = CountTable.Build(new[] { I(1, 100, 0), I(1, 100, -1) });

        Assert.True(table.CanPick(Need((100, 2))));
        Assert.False(table.CanPick(Need((100, 3))));
    }

    [Fact]
    public void CanPickFailsForAMaterialWithNoStackAlsoWithANeedOfZero()
    {
        var table = CountTable.Build(new[] { I(1, 100, 1) });

        Assert.False(table.CanPick(Need((300, 1))));
        Assert.False(table.CanPick(Need((300, 0))));
        Assert.True(table.CanPick(Need((100, 0))));
    }

    [Fact]
    public void TableSetFindsTheTableOfTheSameOwnerIdsInTheirOrder()
    {
        var set = new TableSet();
        var a = CountTable.Build(new[] { I(1, 100, 1) });
        var b = CountTable.Build(new[] { I(2, 100, 2) });
        set.Add(new long[] { 1, 2, 3 }, a);
        set.Add(new long[] { 1, 2 }, b);

        // The buffer is reused, so it can be longer than the owner list.
        Assert.Same(a, set.Find(new long[] { 1, 2, 3, 99 }, 3));
        Assert.Same(b, set.Find(new long[] { 1, 2, 3, 99 }, 2));
        Assert.Null(set.Find(new long[] { 3, 2, 1 }, 3));
        Assert.Null(set.Find(new long[] { 1, 2, 2 }, 3));
        Assert.Null(set.Find(new long[] { 1 }, 1));
    }

    [Fact]
    public void CanPickFailsWithNoItemInThePlaces()
    {
        var table = CountTable.Build(new CountTable.Item[0]);

        Assert.False(table.CanPick(Need()));
        Assert.True(CountTable.Build(new[] { I(1, 100, 1) }).CanPick(Need()));
    }
}
