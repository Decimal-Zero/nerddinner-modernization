using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NerdDinner.Proxy.Models;
using NerdDinner.Proxy.Tests.TestSupport;
using Xunit;

namespace NerdDinner.Proxy.Tests
{
    // Category=Integration: real end-to-end verification of M10's
    // acceptance criteria (plan.md) -- "session handling across the proxy
    // boundary during the transition period ... explicitly tested, not
    // assumed." Uses a real HttpClient with cookie handling enabled
    // (CookieContainer), through the real ASP.NET Core Identity + cookie
    // authentication pipeline, not a fake auth scheme like
    // ViewRenderingTests -- this is specifically testing that the cookie
    // Account/Register or Account/Login issues is honored by a
    // completely different, [Authorize]-gated controller
    // (DinnersController) in the same app, which is the actual thing
    // M8/M9 could never do (decision-log.md DL-022/DL-028) and M10 fixes.
    //
    // Uses a dedicated Identity test database (see
    // ProxyIdentityTestDatabaseFixture) so this suite's throwaway
    // registered users never land in the shared dev "NerdDinner.Identity"
    // database. NerdDinnerCoreContext (Dinners) is left pointed at the
    // real shared dev database, same as this project's other
    // WebApplicationFactory-based tests -- Details/Index on a handful of
    // real seeded dinners is harmless and consistent with existing
    // precedent (HomeRoutingTests.DinnersIndex_IsServedByTheNewApp_NotProxied).
    [Trait("Category", "Integration")]
    [Collection("NerdDinner.Proxy Identity LocalDB collection")]
    public class AuthFlowTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public AuthFlowTests(ProxyIdentityTestDatabaseFixture identityFixture, WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(ProxyIdentityTestDatabaseFixture.ConnectionString));
                });
            });
        }

        // This test writes to the shared dev "NerdDinner" database --
        // NerdDinnerCoreContext isn't overridden to a test-only database
        // the way ApplicationDbContext is (see the constructor above),
        // matching the app's own appsettings.json "NerdDinnerContext"
        // connection string exactly so this points at the same database
        // the WebApplicationFactory-hosted app actually uses.
        private static NerdDinnerCoreContext CreateSharedDevDbContext()
        {
            var options = new DbContextOptionsBuilder<NerdDinnerCoreContext>()
                .UseSqlServer("Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=NerdDinner;Integrated Security=True;MultipleActiveResultSets=True",
                    sql => sql.UseNetTopologySuite())
                .Options;
            return new NerdDinnerCoreContext(options);
        }

        private static async Task<string> ExtractAntiforgeryTokenAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var marker = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
            var start = body.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
            var end = body.IndexOf('"', start);
            return body.Substring(start, end - start);
        }

        [Fact]
        public async Task RegisteredUser_SessionIsRecognized_ByADifferentAlreadyMigratedController()
        {
            // WebApplicationFactory's default client already handles
            // cookies and follows redirects automatically (like a real
            // browser session) -- no custom handler needed.
            var client = _factory.CreateClient();

            var userName = "af" + Guid.NewGuid().ToString("N").Substring(0, 10);

            // Register, via the real Account controller.
            var getRegister = await client.GetAsync("/Account/Register");
            var token = await ExtractAntiforgeryTokenAsync(getRegister);

            var registerForm = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UserName"] = userName,
                ["Password"] = "password1",
                ["ConfirmPassword"] = "password1",
                ["__RequestVerificationToken"] = token,
            });
            var registerResponse = await client.PostAsync("/Account/Register", registerForm);
            Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode); // followed the post-register redirect to "/"

            // The real test: DinnersController.Create is [Authorize]-gated
            // and lives in a completely different controller than
            // Account -- if this 200s with the real Create form (not a
            // redirect to /Account/Login), the authentication cookie
            // Register issued is genuinely recognized elsewhere in the
            // same app, not just by the controller that issued it.
            var createResponse = await client.GetAsync("/Dinners/Create");
            var createBody = await createResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
            Assert.Contains("Host a Dinner", createBody);
            Assert.Contains(userName, createBody); // HostedBy pre-filled from the session
        }

        [Fact]
        public async Task EditView_RendersForRealOwner_AfterRealCreateAndOwnershipCheck()
        {
            // Supersedes the M9-era ViewRenderingTests.DinnersEdit_RendersWithoutError_ForHostUser
            // (which used a fake TestAuthHandler scheme -- retired here,
            // decision-log.md DL-029, since it doesn't compose cleanly
            // with ASP.NET Core Identity's own scheme setup and this test
            // now exercises the identical concern for real). Registers a
            // user, creates a dinner as them (exercising the real
            // Create write path end to end, not just its GET), then
            // confirms Edit's ownership check passes and the Edit form's
            // EditorTemplates (Point, LocationDetail, CountryDropDown)
            // render without a server-side exception -- rather than
            // depending on the shared dev database's seed data.
            var client = _factory.CreateClient();
            var userName = "af" + Guid.NewGuid().ToString("N").Substring(0, 10);

            var getRegister = await client.GetAsync("/Account/Register");
            var registerToken = await ExtractAntiforgeryTokenAsync(getRegister);
            await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UserName"] = userName,
                ["Password"] = "password1",
                ["ConfirmPassword"] = "password1",
                ["__RequestVerificationToken"] = registerToken,
            }));

            var getCreate = await client.GetAsync("/Dinners/Create");
            var createToken = await ExtractAntiforgeryTokenAsync(getCreate);
            // Title has [StringLength(50)] -- "AF " (3) + 32-char GUID hex = 35, well within it.
            var dinnerTitle = "AF " + Guid.NewGuid().ToString("N");
            var createResponse = await client.PostAsync("/Dinners/Create", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Title"] = dinnerTitle,
                ["EventDate"] = "2026-11-01T18:00",
                ["Description"] = "Created by AuthFlowTests",
                ["HostedBy"] = userName,
                ["ContactPhone"] = "555-0100",
                ["Address"] = "1 Test St",
                ["Country"] = "USA",
                ["__RequestVerificationToken"] = createToken,
            }));
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode); // followed the redirect to /Dinners

            using (var db = CreateSharedDevDbContext())
            {
                var dinner = db.Dinners.First(d => d.Title == dinnerTitle);

                var editResponse = await client.GetAsync($"/Dinners/Edit/{dinner.DinnerID}");
                var editBody = await editResponse.Content.ReadAsStringAsync();

                Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);
                Assert.Contains("Host a Dinner", editBody);
                Assert.DoesNotContain("only the host of a Dinner", editBody);

                // Clean up -- this test writes to the shared dev database
                // (NerdDinnerCoreContext isn't overridden the way
                // ApplicationDbContext is; see the class comment).
                db.Dinners.Remove(dinner);
                db.SaveChanges();
            }
        }

        [Fact]
        public async Task UnauthenticatedRequest_ToProtectedRoute_RedirectsToLogin()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var response = await client.GetAsync("/Dinners/Create");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/Login", response.Headers.Location.ToString());
        }
    }
}
