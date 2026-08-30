using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NerdDinner.Proxy.Tests.TestSupport
{
    // Standard ASP.NET Core testing pattern: a fake authentication scheme
    // that always succeeds as a fixed user, wired in for one specific
    // reason -- confirming the [Authorize]-gated Create/Edit views
    // actually render through the real MVC view engine (Razor syntax
    // errors, missing EditorTemplates, etc. aren't caught by the
    // controller-level tests in Controllers/, which inspect a ViewResult's
    // .Model without ever invoking the view engine). See
    // decision-log.md DL-028.
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";

        // Matches src/Migrations/Configuration.cs's Seed() HostedBy value
        // for the shared dev database's seeded dinners -- lets
        // ViewRenderingTests.DinnersEdit_RendersWithoutError_ForHostUser
        // pass the real ownership check and render the actual Edit form,
        // not just InvalidOwner's trivial static view.
        public const string TestUserName = "sample-host";

        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimTypes.Name, TestUserName) };
            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
