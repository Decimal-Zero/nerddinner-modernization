using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NerdDinner.Proxy.Tests.TestSupport;
using Xunit;

namespace NerdDinner.Proxy.Tests
{
    // Category=Integration: same live-legacy-app precondition as
    // HomeRoutingTests (Create/Edit's shared _Layout still needs
    // /Account/* links to resolve, and the proxy's own health depends on
    // the legacy app being reachable). Confirms the [Authorize]-gated
    // Create/Edit views actually render end to end -- through real HTTP,
    // real MVC view engine, real EditorTemplates -- for a genuinely
    // authenticated request, which the controller-level unit tests in
    // Controllers/ can't do (they never invoke the view engine). See
    // decision-log.md DL-028.
    [Trait("Category", "Integration")]
    public class ViewRenderingTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ViewRenderingTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication(TestAuthHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
                });
            });
        }

        private HttpClient CreateAuthenticatedClient()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TestAuthHandler.SchemeName}");
            return client;
        }

        [Fact]
        public async Task DinnersCreate_RendersWithoutError_ForAuthenticatedUser()
        {
            var client = CreateAuthenticatedClient();

            var response = await client.GetAsync("/Dinners/Create");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Host a Dinner", body);
            // Confirms the Point/LocationDetail EditorTemplates actually
            // resolved and rendered (a missing template throws at render
            // time, which a 200 here rules out) and the pre-filled
            // HostedBy field carries the fake user's name through.
            Assert.Contains(TestAuthHandler.TestUserName, body);
        }

        [Fact]
        public async Task DinnersEdit_RendersWithoutError_ForHostUser()
        {
            var client = CreateAuthenticatedClient();

            // DinnerID 1 ("Sample Dinner: Seattle Nerds") is seeded by
            // src/Migrations/Configuration.cs's Seed() with
            // HostedBy = "sample-host", matching TestAuthHandler's fake
            // user -- so this passes the real ownership check and renders
            // the actual Edit form (not InvalidOwner's static view),
            // confirming its EditorTemplates render for a populated
            // Location too, not just Create's empty one.
            var response = await client.GetAsync("/Dinners/Edit/1");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Host a Dinner", body);
            Assert.DoesNotContain("only the host of a Dinner", body);
        }
    }
}
