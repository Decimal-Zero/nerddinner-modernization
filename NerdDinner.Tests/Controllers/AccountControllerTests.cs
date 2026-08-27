using System.Web.Mvc;
using NerdDinner.Controllers;
using Xunit;

namespace NerdDinner.Tests.Controllers
{
    /// <summary>
    /// AccountController's core flows (Login, Register, external OAuth
    /// callback handling) are built directly on SimpleMembership's static
    /// `WebSecurity` class and `OAuthWebSecurity`, both of which require a
    /// fully initialized ASP.NET runtime (the [InitializeSimpleMembershipAttribute]
    /// on the controller, database-backed membership tables, and -- for
    /// the OAuth flows -- DotNetOpenAuth's own request-context plumbing).
    /// None of that is mockable through a seam; it's ambient static state
    /// tied to a real HTTP pipeline.
    ///
    /// Rather than force-fitting brittle unit tests around static-class
    /// internals that M4 is replacing outright (per DL-006, in Phase 1 --
    /// SimpleMembership + DotNetOpenAuth are being swapped for ASP.NET
    /// Identity + OWIN), the more honest allocation of effort is:
    ///   1. Characterize what IS independently testable here (the two
    ///      simple, side-effect-free view-returning actions below).
    ///   2. Characterize the OBSERVABLE authentication contract
    ///      (successful login redirects where expected, failed login
    ///      redisplays the form with an error, registration creates an
    ///      account) via a higher-level integration test once M4 lands
    ///      the new auth stack -- at that point there's a real
    ///      opportunity to design it for testability from the start,
    ///      rather than reverse-engineering seams into code about to be
    ///      deleted.
    ///
    /// This is a deliberate, logged coverage gap, not an oversight.
    /// </summary>
    public class AccountControllerTests
    {
        [Fact]
        public void ExternalLoginFailure_ReturnsDefaultView()
        {
            var controller = new AccountController();

            var result = controller.ExternalLoginFailure() as ViewResult;

            Assert.True(string.IsNullOrEmpty(result.ViewName));
        }

        // Register(GET), Login(GET), and Manage(GET) are simple
        // `return View();` actions with no logic of their own to
        // characterize -- omitted rather than padded out with
        // no-op assertions.
    }
}
