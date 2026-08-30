using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace NerdDinner.Tests
{
    // Ported forward from the strangler-fig era (M8-M10): originally
    // proved the routing decision between "served by the new app" and
    // "forwarded to the legacy app" through a live YARP reverse proxy.
    // M11 (decision-log.md DL-031) removed the legacy app and the proxy
    // entirely -- there's nothing left to distinguish "served" from
    // "not proxied," so these are now plain smoke tests confirming each
    // route responds correctly through the real ASP.NET Core pipeline
    // (WebApplicationFactory + real HTTP, not controllers in isolation).
    // No longer Category=Integration -- that tag was specifically about
    // needing the legacy app running under IIS Express, a precondition
    // that no longer exists.
    public class HomeRoutingTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public HomeRoutingTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async System.Threading.Tasks.Task Root_ServesHomeIndex()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("nerds and helping them eat in packs", body);
        }

        [Fact]
        public async System.Threading.Tasks.Task HomeAbout_Serves()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/Home/About");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("What is NerdDinner.com?", body);
        }

        [Fact]
        public async System.Threading.Tasks.Task DinnersIndex_Serves()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/Dinners");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Upcoming Dinners", body);
        }

        [Fact]
        public async System.Threading.Tasks.Task SearchApi_Serves()
        {
            var client = _factory.CreateClient();

            // POST api/Search?limit=... is NerdDinner.js's own call shape
            // (NerdDinner.FindMostPopularDinners).
            var response = await client.PostAsync("/api/Search?limit=5", null);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.StartsWith("[", body.Trim());
        }

        [Fact]
        public async System.Threading.Tasks.Task NonexistentRoute_Returns404_NotSwallowedByAFallback()
        {
            // With the YARP catch-all gone (M11), a request that matches
            // no controller/action should 404 cleanly rather than being
            // silently forwarded anywhere.
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/ThisRouteDoesNotExist12345");

            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
