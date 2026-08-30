using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using NerdDinner.Controllers;
using NerdDinner.Services;
using NerdDinner.Tests.TestSupport;
using Xunit;

namespace NerdDinner.Tests.Controllers
{
    // Ported from NerdDinner.Tests.Controllers.SearchControllerTests (M9,
    // decision-log.md DL-028), plus real coverage of the spatial distance
    // query (SearchByLocation/FindByLocation) that the legacy suite could
    // never exercise -- GeolocationService there was a static class
    // tightly coupled to ConfigurationManager, with no seam to construct
    // it independently of a live network call. Here it's constructor
    // injectable, so the network-free half of Search (the actual spatial
    // query -- the "first real exercise of this new data layer" plan.md's
    // M9 acceptance criteria calls out) is directly testable. The
    // geocoding-driven half (SearchByPlaceNameOrZip) still needs a live
    // GeoNames call and stays untested here for the same reason as the
    // legacy suite's documented gap.
    [Collection("NerdDinner LocalDB collection")]
    public class SearchControllerTests
    {
        public SearchControllerTests(TestDatabaseFixture fixture)
        {
            fixture.Reset();
        }

        private static SearchController CreateController()
        {
            var db = TestDatabaseFixture.CreateContext();
            var configuration = new ConfigurationBuilder().Build();
            var geolocationService = new GeolocationService(configuration, new MemoryCache(new MemoryCacheOptions()));
            return new SearchController(db, geolocationService);
        }

        [Fact]
        public void SearchByLocation_FindsNearbyDinner_WithinDistanceThreshold()
        {
            var controller = CreateController();

            // Exact seed coordinates for "Alice's Dinner"/"Bob's Dinner"/
            // "Past Dinner" (downtown Seattle) -- see TestDatabase.cs.
            var result = controller.SearchByLocation(47.608013, -122.335167).ToList();

            var titles = result.Select(d => d.Title).ToList();
            Assert.Contains("Alice's Dinner", titles);
            Assert.Contains("Bob's Dinner", titles);
        }

        [Fact]
        public void SearchByLocation_ExcludesDinner_OutsideDistanceThreshold()
        {
            var controller = CreateController();

            // Same query point as above -- "Portland Dinner" is seeded
            // ~250km away, well outside the 2000m radius.
            var result = controller.SearchByLocation(47.608013, -122.335167).ToList();

            Assert.DoesNotContain(result, d => d.Title == "Portland Dinner");
        }

        [Fact]
        public void SearchByLocation_ReturnsCorrectCoordinates_ForMatchedDinner()
        {
            // Confirms the round trip through the geography column and
            // back out as JsonDinner.Latitude/Longitude is exact, not
            // just "a query executed" -- the M9 acceptance criteria's
            // specific concern about location storage/retrieval.
            var controller = CreateController();

            var result = controller.SearchByLocation(47.608013, -122.335167).ToList();
            var dinner = result.First(d => d.Title == "Alice's Dinner");

            Assert.Equal(47.608013, dinner.Latitude, precision: 5);
            Assert.Equal(-122.335167, dinner.Longitude, precision: 5);
        }

        [Fact]
        public void SearchByPlaceNameOrZip_ReturnsNull_ForEmptyLocation()
        {
            var controller = CreateController();

            var result = controller.SearchByPlaceNameOrZip(location: "");

            Assert.Null(result);
        }

        [Fact]
        public void SearchByPlaceNameOrZip_ReturnsNull_ForNullLocation()
        {
            var controller = CreateController();

            var result = controller.SearchByPlaceNameOrZip(location: null);

            Assert.Null(result);
        }

        [Fact]
        public void GetMostPopularDinners_OrdersByRSVPCountDescending()
        {
            var controller = CreateController();

            var result = controller.GetMostPopularDinners(limit: 10).ToList();

            var bobIndex = result.FindIndex(d => d.Title == "Bob's Dinner");
            var aliceIndex = result.FindIndex(d => d.Title == "Alice's Dinner");

            if (bobIndex >= 0 && aliceIndex >= 0)
            {
                Assert.True(bobIndex < aliceIndex);
            }
        }

        [Fact]
        public void GetMostPopularDinners_ExcludesPastDinners()
        {
            var controller = CreateController();

            var result = controller.GetMostPopularDinners(limit: 10).ToList();

            Assert.DoesNotContain(result, d => d.Title == "Past Dinner");
        }

        [Fact]
        public void JsonDinnerFromDinner_UsesZeroZeroCoordinates_WhenDinnerHasNoLocation()
        {
            // Deliberate behavior change, not a silent one -- see
            // decision-log.md DL-032. This used to be a preserved
            // characterized bug (DL-004/DL-028): JsonDinnerFromDinner
            // dereferenced dinner.Location unconditionally, throwing an
            // NRE for any dinner with no Location (legal by the model's
            // own contract; see DinnerTests). With Bing Maps' free tier
            // retired and no in-plan replacement (DL-030), a dinner
            // created without a geocoded address stopped being a rare
            // edge case and started being the common path -- an NRE here
            // took down the front page's "Popular Dinners" list (and any
            // other Search-backed page) every time. Now uses (0, 0) as a
            // placeholder instead of throwing.
            using (var db = TestDatabaseFixture.CreateContext())
            {
                db.Dinners.Add(new Models.Dinner
                {
                    Title = "No Location Dinner",
                    EventDate = DateTime.Now.AddDays(1),
                    Description = "Missing spatial data",
                    HostedBy = "alice",
                    ContactPhone = "555-0199",
                    Address = "Nowhere",
                    Country = "USA",
                    Location = null
                });
                db.SaveChanges();
            }

            var controller = CreateController();

            var result = controller.GetMostPopularDinners(limit: 10).ToList();
            var dinner = result.First(d => d.Title == "No Location Dinner");

            Assert.Equal(0, dinner.Latitude);
            Assert.Equal(0, dinner.Longitude);
        }
    }
}
