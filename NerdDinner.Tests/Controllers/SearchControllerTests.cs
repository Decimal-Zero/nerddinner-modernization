using NerdDinner.Controllers;
using NerdDinner.Models;
using NerdDinner.Tests.TestSupport;
using System;
using System.Linq;
using Xunit;

namespace NerdDinner.Tests.Controllers
{
    [Collection("NerdDinner LocalDB collection")]
    public class SearchControllerTests
    {
        public SearchControllerTests(TestDatabaseFixture fixture)
        {
            fixture.Reset();
        }

        [Fact]
        public void SearchByPlaceNameOrZip_ReturnsNull_ForEmptyLocation()
        {
            // The one branch of this action testable without a live
            // network call -- it short-circuits before ever reaching
            // GeolocationService.
            var controller = new SearchController();

            var result = controller.SearchByPlaceNameOrZip(location: "");

            Assert.Null(result);
        }

        [Fact]
        public void SearchByPlaceNameOrZip_ReturnsNull_ForNullLocation()
        {
            var controller = new SearchController();

            var result = controller.SearchByPlaceNameOrZip(location: null);

            Assert.Null(result);
        }

        [Fact]
        public void GetMostPopularDinners_OrdersByRSVPCountDescending()
        {
            var controller = new SearchController();

            var result = controller.GetMostPopularDinners(limit: 10).ToList();

            // "Bob's Dinner" (2 RSVPs) should outrank "Alice's Dinner" (0)
            // if both appear.
            var bobIndex = result.FindIndex(d => d.Title == "Bob's Dinner");
            var aliceIndex = result.FindIndex(d => d.Title == "Alice's Dinner");
            System.Diagnostics.Trace.WriteLine($"Bob index: {bobIndex}, Alice index: {aliceIndex}");

            if (bobIndex >= 0 && aliceIndex >= 0)
            {
                Assert.True(bobIndex < aliceIndex);
            }
        }

        [Fact]
        public void GetMostPopularDinners_ExcludesPastDinners()
        {
            var controller = new SearchController();

            var result = controller.GetMostPopularDinners(limit: 10).ToList();

            Assert.DoesNotContain(result, d => d.Title == "Past Dinner");
        }

        [Fact]
        public void JsonDinnerFromDinner_ThrowsNullReferenceException_WhenDinnerHasNoLocation()
        {
            // Found while building this fixture: JsonDinnerFromDinner
            // dereferences dinner.Location.Latitude/.Longitude
            // unconditionally. A dinner with no Location set (which the
            // model layer permits -- Location has no [Required]
            // attribute, see DinnerTests) throws NRE the moment it's
            // serialized to JSON, rather than omitting coordinates or
            // filtering the dinner out. Characterized directly here via
            // GetMostPopularDinners, the code path that hits it.
            using (var db = new NerdDinnerContext())
            {
                db.Dinners.Add(new Dinner
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

            var controller = new SearchController();

            Assert.Throws<NullReferenceException>(() => controller.GetMostPopularDinners(limit: 10).ToList());
        }

        // --- Documented gap, not a test: SearchByPlaceNameOrZip's
        // geocoding-driven branch, and SearchByLocation/FindByLocation's
        // DbGeography.Distance() spatial query, both require either a
        // live call to GeolocationService's external APIs or a seam that
        // doesn't exist in this codebase (GeolocationService is a static
        // class tightly coupled to ConfigurationManager and a static
        // MemoryCache -- see the assessment's Architecture/Category 2
        // coupling finding). Characterizing these paths as true unit
        // tests isn't possible without either accepting network flakiness
        // in the test suite or introducing a seam, which is itself a
        // (non-behavioral) code change this milestone isn't scoped to
        // make. See GeolocationServiceTests.cs for the same limitation,
        // handled there via explicitly-tagged integration tests instead.
    }
}
