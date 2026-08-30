using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NerdDinner.Proxy.Models;
using NerdDinner.Proxy.Tests.TestSupport;
using Xunit;

namespace NerdDinner.Proxy.Tests.Controllers
{
    // Ported from NerdDinner.Tests.Controllers.AccountControllerIdentityTests
    // (M10, decision-log.md DL-029) -- same observable authentication
    // contract characterized (registration succeeds, duplicate names
    // rejected, short passwords rejected, login accepts correct
    // credentials and rejects wrong ones), now against ASP.NET Core
    // Identity instead of ASP.NET Identity 2.x/EF6. Builds a real DI
    // container with the exact same AddIdentity(...) configuration
    // Program.cs uses (same password/username policy), rather than
    // hand-constructing UserManager -- UserManager's real constructor
    // has several required collaborators (validators, a password hasher,
    // a key normalizer, etc.) that DI wires up correctly and a hand-built
    // instance easily gets subtly wrong.
    [Collection("NerdDinner.Proxy Identity LocalDB collection")]
    public class AccountControllerIdentityTests
    {
        public AccountControllerIdentityTests(ProxyIdentityTestDatabaseFixture fixture)
        {
        }

        private static UserManager<ApplicationUser> CreateUserManager()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(ProxyIdentityTestDatabaseFixture.ConnectionString));
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    // Same policy as Program.cs -- matches the legacy
                    // app's ApplicationUserManager.Create policy in turn
                    // (decision-log.md DL-014).
                    options.Password.RequiredLength = 6;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.User.AllowedUserNameCharacters = null;
                    options.User.RequireUniqueEmail = false;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<UserManager<ApplicationUser>>();
        }

        [Fact]
        public async Task Create_Succeeds_ForNewUserNameAndValidPassword()
        {
            var manager = CreateUserManager();
            var user = new ApplicationUser { UserName = "newuser_" + Guid.NewGuid().ToString("N") };

            var result = await manager.CreateAsync(user, "password1");

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task Create_Fails_ForDuplicateUserName()
        {
            var manager = CreateUserManager();
            string userName = "dupeuser_" + Guid.NewGuid().ToString("N");
            await manager.CreateAsync(new ApplicationUser { UserName = userName }, "password1");

            var result = await manager.CreateAsync(new ApplicationUser { UserName = userName }, "password1");

            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task Create_Fails_ForPasswordShorterThanSixCharacters()
        {
            var manager = CreateUserManager();
            var user = new ApplicationUser { UserName = "shortpw_" + Guid.NewGuid().ToString("N") };

            var result = await manager.CreateAsync(user, "abc12");

            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task CheckPassword_ReturnsTrue_ForCorrectPassword()
        {
            var manager = CreateUserManager();
            string userName = "logintest_" + Guid.NewGuid().ToString("N");
            var user = new ApplicationUser { UserName = userName };
            await manager.CreateAsync(user, "correcthorse");

            var found = await manager.FindByNameAsync(userName);
            var passwordOk = await manager.CheckPasswordAsync(found, "correcthorse");

            Assert.True(passwordOk);
        }

        [Fact]
        public async Task CheckPassword_ReturnsFalse_ForIncorrectPassword()
        {
            var manager = CreateUserManager();
            string userName = "badpwtest_" + Guid.NewGuid().ToString("N");
            var user = new ApplicationUser { UserName = userName };
            await manager.CreateAsync(user, "correcthorse");

            var found = await manager.FindByNameAsync(userName);
            var passwordOk = await manager.CheckPasswordAsync(found, "wrongpassword");

            Assert.False(passwordOk);
        }

        [Fact]
        public async Task FindByName_ReturnsNull_ForNonexistentUserName()
        {
            var manager = CreateUserManager();

            var found = await manager.FindByNameAsync("no-such-user_" + Guid.NewGuid().ToString("N"));

            Assert.Null(found);
        }
    }
}
