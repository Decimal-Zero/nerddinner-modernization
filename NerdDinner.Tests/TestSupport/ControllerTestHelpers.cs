using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NerdDinner.Tests.TestSupport
{
    // ASP.NET Core equivalent of the legacy test project's
    // ControllerTestHelpers.SetFakeUser -- same approach (instantiate the
    // controller directly, fake the current user on its context) and same
    // caveat: this doesn't exercise the real [Authorize] filter pipeline,
    // it characterizes what the action does once invoked.
    public static class ControllerTestHelpers
    {
        public static void SetFakeUser(this ControllerBase controller, string userName)
        {
            var identity = userName != null
                ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, userName) }, "TestAuth")
                : new ClaimsIdentity();

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }
    }
}
