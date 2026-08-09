using Xunit;

namespace FSO.PackCompiler.Tests
{
    public class GuidAllocatorTests
    {
        [Fact]
        public void SamePackAndObjectId_ProducesSameGuidAcrossCalls()
        {
            var a = GuidAllocator.Allocate("gossip-gnome", "gossip_gnome");
            var b = GuidAllocator.Allocate("gossip-gnome", "gossip_gnome");
            Assert.Equal(a, b);
        }

        [Fact]
        public void DifferentObjectId_UsuallyProducesDifferentGuid()
        {
            var a = GuidAllocator.Allocate("fortune-cat", "fortune_cat");
            var b = GuidAllocator.Allocate("fortune-cat", "fortune_cat_v2");
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void DifferentPackId_SameObjectId_UsuallyProducesDifferentGuid()
        {
            var a = GuidAllocator.Allocate("pack-a", "teapot");
            var b = GuidAllocator.Allocate("pack-b", "teapot");
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void ConcatenationBoundary_DoesNotCollide()
        {
            // ("ab","c") and ("a","bc") must not hash to the same GUID just because a naive
            // concatenation would produce the same string.
            var a = GuidAllocator.Allocate("ab", "c");
            var b = GuidAllocator.Allocate("a", "bc");
            Assert.NotEqual(a, b);
        }

        [Theory]
        [InlineData("gossip-gnome", "gossip_gnome")]
        [InlineData("fortune-cat", "fortune_cat")]
        [InlineData("wishing-well", "wishing_well")]
        [InlineData("", "")]
        public void AlwaysFallsInsideCommunityRange(string packId, string objectId)
        {
            var guid = GuidAllocator.Allocate(packId, objectId);
            Assert.InRange(guid, GuidAllocator.CommunityRangeStart, GuidAllocator.CommunityRangeEnd);
        }
    }
}
