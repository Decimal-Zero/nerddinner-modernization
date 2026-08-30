using Microsoft.EntityFrameworkCore;
using NerdDinner.Proxy.Models;
using Xunit;

namespace NerdDinner.Proxy.Tests.TestSupport
{
    // Dedicated LocalDB catalog for Identity tests -- separate from both
    // the shared dev "NerdDinner.Identity" database (M10, DL-029) and
    // NerdDinner.Proxy's Dinners-oriented ProxyTestDatabaseFixture, same
    // reasoning as the legacy test project's separate
    // IdentityTestDatabaseFixture/TestDatabaseFixture split: an
    // automated suite creating/dropping throwaway user accounts
    // shouldn't touch the same database a developer is manually
    // exercising the app against.
    public class ProxyIdentityTestDatabaseFixture : IDisposable
    {
        public const string ConnectionString =
            "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=NerdDinnerProxyIdentityTests;Integrated Security=True;MultipleActiveResultSets=True";

        public ProxyIdentityTestDatabaseFixture()
        {
            using var db = CreateContext();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }

        public static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;
            return new ApplicationDbContext(options);
        }

        public void Dispose()
        {
            using var db = CreateContext();
            db.Database.EnsureDeleted();
        }
    }

    [CollectionDefinition("NerdDinner.Proxy Identity LocalDB collection")]
    public class ProxyIdentityDatabaseCollection : ICollectionFixture<ProxyIdentityTestDatabaseFixture>
    {
    }
}
