using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;

namespace NerdDinner.Tests.TestSupport
{
    /// <summary>
    /// Standard-pattern helpers for unit testing classic ASP.NET MVC
    /// controllers by instantiating them directly and invoking action
    /// methods, rather than running a live HTTP pipeline. This is the
    /// well-established approach for this MVC generation (controller
    /// actions are plain methods returning ActionResult) and needs no
    /// IIS/OWIN self-host.
    /// </summary>
    public static class ControllerTestHelpers
    {
        /// <summary>
        /// Attaches a fake ControllerContext to the given controller with
        /// User.Identity.Name set to <paramref name="userName"/> (or
        /// anonymous if null), so [Authorize]-gated logic and
        /// User.Identity.Name-dependent code can be exercised.
        /// Note: this does NOT enforce the [Authorize] filter itself --
        /// that's part of the MVC pipeline, not the controller. These
        /// tests characterize what the ACTION does once invoked, on the
        /// (reasonable) assumption that [Authorize] already gated access.
        /// Filter-level behavior is a known gap, noted in
        /// docs/02-plan/m2-characterization-tests.md.
        /// </summary>
        public static void SetFakeUser(this ControllerBase controller, string userName)
        {
            var identity = new Mock<IIdentity>();
            identity.Setup(i => i.Name).Returns(userName ?? string.Empty);
            identity.Setup(i => i.IsAuthenticated).Returns(userName != null);

            var principal = new Mock<IPrincipal>();
            principal.Setup(p => p.Identity).Returns(identity.Object);

            var httpContext = new Mock<HttpContextBase>();
            httpContext.Setup(c => c.User).Returns(principal.Object);

            var requestContext = new RequestContext(httpContext.Object, new System.Web.Routing.RouteData());
            controller.ControllerContext = new ControllerContext(requestContext, controller);
        }
    }
}
