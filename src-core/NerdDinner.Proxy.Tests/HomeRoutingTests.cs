using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace NerdDinner.Proxy.Tests
{
    // Category=Integration: hosts the real NerdDinner.Proxy app (with its
    // real appsettings.json YARP config) via WebApplicationFactory and sends
    // actual HTTP requests through it, exercising the genuine routing
    // decision between "served by the new app" and "forwarded to the
    // legacy app" — not the new app's controllers in isolation. Requires
    // the legacy NerdDinner app to already be running under IIS Express on
    // localhost:10581 (same precondition as this repo's other
    // Category=Integration tests needing a live external dependency; see
    // CLAUDE.md / m2-characterization-tests.md for the GeoNames/ipinfodb
    // precedent), and is therefore excluded from the default fast run.
    [Trait("Category", "Integration")]
    public class HomeRoutingTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public HomeRoutingTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async System.Threading.Tasks.Task Root_IsServedByTheNewApp_NotProxied()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("nerds and helping them eat in packs", body);
            // Only the new app's _Layout renders this footer text — the
            // legacy app's equivalent renders "Version: 1.0" from
            // Global.asax's Application state. Its presence proves this
            // response came from NerdDinner.Proxy's own MVC pipeline, not
            // a YARP-forwarded copy of the legacy page.
            Assert.Contains("Version: Phase 2 (ASP.NET Core)", body);
        }

        [Fact]
        public async System.Threading.Tasks.Task HomeAbout_IsServedByTheNewApp_NotProxied()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/Home/About");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("What is NerdDinner.com?", body);
            Assert.Contains("Version: Phase 2 (ASP.NET Core)", body);
        }

        [Fact]
        public async System.Threading.Tasks.Task GlimpseAxd_IsStillForwardedToTheLegacyApp()
        {
            var client = _factory.CreateClient();

            // /Dinners (M8), then /Account/Login (M9) were this test's
            // targets in turn, each migrated out from under it by the
            // next milestone. M10 migrated Account too (decision-log.md
            // DL-029) -- every legacy controller (Home, Dinners, RSVP,
            // Search, Account) is now served by the new app, so there's
            // no more unmigrated *feature* route left to prove the
            // fallback against. glimpse.axd is genuinely legacy-only and
            // staying that way -- flagged for removal at M11 (DL-019), not
            // ported -- so it's still a real, honest target for "the YARP
            // catch-all still reaches the legacy app," not a route this
            // test will need to chase again next milestone.
            var response = await client.GetAsync("/glimpse.axd");

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task DinnersIndex_IsServedByTheNewApp_NotProxied()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/Dinners");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Upcoming Dinners", body);
            Assert.Contains("Version: Phase 2 (ASP.NET Core)", body);
        }

        [Fact]
        public async System.Threading.Tasks.Task SearchApi_IsServedByTheNewApp_NotProxied()
        {
            var client = _factory.CreateClient();

            // POST api/Search?limit=... is NerdDinner.js's own call shape
            // (NerdDinner.FindMostPopularDinners) -- confirms the new
            // app's Search route responds with well-formed JSON rather
            // than the legacy app's response (which would also return
            // 200, so status code alone wouldn't distinguish them).
            var response = await client.PostAsync("/api/Search?limit=5", null);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.StartsWith("[", body.Trim());
        }

        [Fact]
        public async System.Threading.Tasks.Task ProxyHealthEndpoint_IsServedByTheNewApp()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/_proxy/health");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("NerdDinner.Proxy (ASP.NET Core) is alive", body);
        }
    }
}
