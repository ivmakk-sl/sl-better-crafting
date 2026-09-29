using System.Collections.Generic;
using Xunit;

namespace BetterCrafting.Tests
{
    public class AnyStorageLogicTests
    {
        private static readonly ISet<long> NoExcluded = new HashSet<long>();

        [Fact]
        public void HomeOwnersFollowTheGameOwnersInTheirOrder()
        {
            var owners = AnyStorageLogic.AppendOwners(new long[] { 5, 1, 4 }, new long[] { 30, 20 }, NoExcluded);
            Assert.Equal(new long[] { 5, 1, 4, 30, 20 }, owners);
        }

        [Fact]
        public void ZeroAndExcludedHomeOwnersAreLeftOut()
        {
            var owners = AnyStorageLogic.AppendOwners(new long[] { 1 }, new long[] { 0, 7, 30, 8 }, new HashSet<long> { 7, 8 });
            Assert.Equal(new long[] { 1, 30 }, owners);
        }

        [Fact]
        public void HomeOwnerAlreadyInTheListGoesInOnce()
        {
            var owners = AnyStorageLogic.AppendOwners(new long[] { 1, 4 }, new long[] { 4, 30, 30, 1 }, NoExcluded);
            Assert.Equal(new long[] { 1, 4, 30 }, owners);
        }

        [Fact]
        public void NoHomeOwnerKeepsTheGameOwners()
        {
            var owners = AnyStorageLogic.AppendOwners(new long[] { 5, 1, 4 }, new long[0], NoExcluded);
            Assert.Equal(new long[] { 5, 1, 4 }, owners);
        }
    }
}
