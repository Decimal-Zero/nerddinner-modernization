using System;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using NerdDinner.Controllers;
using NerdDinner.Models;
using NerdDinner.Tests.TestSupport;
using Xunit;

namespace NerdDinner.Tests.Controllers
{
    /// <summary>
    /// AccountController's action-level flows (ExternalLogin challenge,
    /// external callback, Manage/Disassociate) still require a live OWIN
    /// context -- HttpContext.GetOwinContext() -- that isn't mockable
    /// through a seam without introducing an abstraction the controller
    /// doesn't otherwise need. Those stay characterized only at the
    /// side-effect-free level below, same as before M4.
    ///
    /// What M4 DOES make testable, and wasn't before: the actual
    /// authentication mechanics (ApplicationUserManager, backed by EF6's
    /// UserStore&lt;ApplicationUser&gt;) have a real seam now, unlike
    /// SimpleMembership's WebSecurity static class. AccountControllerIdentityTests
    /// below exercises that directly against LocalDB -- this is the
    /// "observable authentication contract" (registration succeeds,
    /// duplicate names rejected, login accepts correct credentials and
    /// rejects wrong ones) this file's comment promised once M4 landed.
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

    [Collection("NerdDinner Identity LocalDB collection")]
    public class AccountControllerIdentityTests
    {
        private static UserManager<ApplicationUser> CreateUserManager()
        {
            var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext(TestConnectionStrings.Get("DefaultConnection"))));

            // Same policy AccountController's real ApplicationUserManager.Create
            // configures (App_Start/IdentityConfig.cs) -- matching
            // SimpleMembership's old length-only password gate rather than
            // Identity's stricter defaults.
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = false,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false,
            };

            return manager;
        }

        [Fact]
        public void Create_Succeeds_ForNewUserNameAndValidPassword()
        {
            var manager = CreateUserManager();
            var user = new ApplicationUser { UserName = "newuser_" + Guid.NewGuid().ToString("N") };

            var result = manager.Create(user, "password1");

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Create_Fails_ForDuplicateUserName()
        {
            var manager = CreateUserManager();
            string userName = "dupeuser_" + Guid.NewGuid().ToString("N");
            manager.Create(new ApplicationUser { UserName = userName }, "password1");

            var result = manager.Create(new ApplicationUser { UserName = userName }, "password1");

            Assert.False(result.Succeeded);
        }

        [Fact]
        public void Create_Fails_ForPasswordShorterThanSixCharacters()
        {
            var manager = CreateUserManager();
            var user = new ApplicationUser { UserName = "shortpw_" + Guid.NewGuid().ToString("N") };

            var result = manager.Create(user, "abc12");

            Assert.False(result.Succeeded);
        }

        [Fact]
        public void Find_ReturnsUser_ForCorrectUsernameAndPassword()
        {
            var manager = CreateUserManager();
            string userName = "logintest_" + Guid.NewGuid().ToString("N");
            manager.Create(new ApplicationUser { UserName = userName }, "correcthorse");

            var found = manager.Find(userName, "correcthorse");

            Assert.NotNull(found);
        }

        [Fact]
        public void Find_ReturnsNull_ForIncorrectPassword()
        {
            var manager = CreateUserManager();
            string userName = "badpwtest_" + Guid.NewGuid().ToString("N");
            manager.Create(new ApplicationUser { UserName = userName }, "correcthorse");

            var found = manager.Find(userName, "wrongpassword");

            Assert.Null(found);
        }

        [Fact]
        public void Find_ReturnsNull_ForNonexistentUserName()
        {
            var manager = CreateUserManager();

            var found = manager.Find("no-such-user_" + Guid.NewGuid().ToString("N"), "whatever1");

            Assert.Null(found);
        }
    }
}
