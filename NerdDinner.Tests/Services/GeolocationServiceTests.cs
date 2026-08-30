using System;
using NerdDinner.Services;
using NerdDinner.Tests.TestSupport;
using Xunit;

namespace NerdDinner.Tests.Services
{
    /// <summary>
    /// GeolocationService is a static class, tightly coupled directly to
    /// ConfigurationManager and a static MemoryCache (the assessment's
    /// Architecture/Category 2 coupling finding). There is no seam to
    /// inject a fake HTTP responder, so a true, network-free unit test of
    /// its failure paths isn't possible without changing the code -- and
    /// this milestone's job is to characterize behavior BEFORE any change,
    /// not to refactor for testability first.
    ///
    /// These tests are tagged "Integration" and hit the real, live
    /// third-party APIs (geonames.org, ipinfodb.com). They are NOT part
    /// of the default fast test run -- exclude the "Integration" trait
    /// when running the suite day to day:
    ///
    ///   vstest.console.exe NerdDinner.Tests.dll /TestCaseFilter:"Category!=Integration"
    ///
    /// or in Visual Studio's Test Explorer, group by Trait and deselect
    /// "Integration". Run them deliberately, occasionally, to confirm the
    /// characterization still holds -- these are the tests most likely to
    /// break for reasons that have nothing to do with this codebase (rate
    /// limits, the third-party services changing their response format,
    /// or going away entirely).
    /// 
    /// Confirmed on 27-Aug-2026: The subdomain "ws.geonames.org", which
    /// GeolocationService.PlaceOrZipToLatLong() uses is no longer available
    /// and there is no redirect. The replacement is "api.geonames.org" and
    /// is a drop in replacement.
    /// </summary>
    public class GeolocationServiceTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public void PlaceOrZipToLatLong_ReturnsCoordinates_ForKnownValidZip()
        {
            var result = GeolocationService.PlaceOrZipToLatLong("98101", TestAppSettings.Get("GeoNames:UserName")); // Seattle

            Assert.NotNull(result);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void PlaceOrZipToLatLong_ReturnsNull_WhenNoResultsFound()
        {
            // Characterizing the "no match" path specifically, as distinct
            // from the "API unreachable" path below -- geonames.org
            // returning zero results is handled gracefully (returns null),
            // per the existing `if (result.Descendants("code").Any())`
            // check.
            var result = GeolocationService.PlaceOrZipToLatLong("zzzznotarealplacezzzz", TestAppSettings.Get("GeoNames:UserName"));

            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void HostIpToPlaceName_ReturnsNull_WhenApiKeyIsBlank()
        {
            // Deliberate M5 behavior change, not a regression -- see
            // decision-log.md DL-016 and plan.md M5. Before M5,
            // HostIpToPlaceName had no try/catch and no null-check around
            // `.First()`, so a blank ipInfoDbKey (the checked-in Web.config
            // ships this blank) propagated whatever exception the failed
            // ipinfodb.com call produced straight to the caller. M5 wraps
            // the external call and returns null on any failure instead,
            // matching PlaceOrZipToLatLong's existing "no match" contract.
            var result = GeolocationService.HostIpToPlaceName("127.0.0.1", TestAppSettings.Get("ipInfoDbKey"));

            Assert.Null(result);
        }
    }
}
