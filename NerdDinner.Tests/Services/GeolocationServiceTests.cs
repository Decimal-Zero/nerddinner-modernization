using System;
using NerdDinner.Services;
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
        [Fact(Skip = "Domain no longer valid.")]
        [Trait("Category", "Integration")]
        public void PlaceOrZipToLatLong_ReturnsCoordinates_ForKnownValidZip()
        {
            var result = GeolocationService.PlaceOrZipToLatLong("98101"); // Seattle

            Assert.NotNull(result);
        }

        [Fact(Skip = "Domain no longer valid.")]
        [Trait("Category", "Integration")]
        public void PlaceOrZipToLatLong_ReturnsNull_WhenNoResultsFound()
        {
            // Characterizing the "no match" path specifically, as distinct
            // from the "API unreachable" path below -- geonames.org
            // returning zero results is handled gracefully (returns null),
            // per the existing `if (result.Descendants("code").Any())`
            // check.
            var result = GeolocationService.PlaceOrZipToLatLong("zzzznotarealplacezzzz");

            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void HostIpToPlaceName_ThrowsUnhandledException_WhenApiKeyIsBlank()
        {
            // Characterizing real, current (bad) behavior per DL-004: with
            // no ipInfoDbKey configured (the checked-in Web.config ships
            // this blank, per the assessment's Category 5 finding that
            // secrets are at least correctly externalized), the ipinfodb.com
            // call either fails outright or returns a response this method
            // doesn't handle gracefully -- there is no try/catch and no
            // null-check around `.First()`. Whatever the specific exception
            // type turns out to be against the live API today, this method
            // is EXPECTED to throw without a valid key, and that's the
            // point being documented: not a "correct" error message, just
            // that failure here is currently unhandled and propagates
            // straight to the caller (SearchController, if this were ever
            // wired to it -- currently it isn't called from any controller
            // in this codebase).
            Assert.ThrowsAny<Exception>(() => GeolocationService.HostIpToPlaceName("127.0.0.1"));
        }
    }
}
