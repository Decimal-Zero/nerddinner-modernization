using Microsoft.EntityFrameworkCore;
using NerdDinner.Models;
using Xunit;

namespace NerdDinner.Tests.TestSupport
{
    // Dedicated LocalDB catalog for Identity tests -- separate from both
    // the shared dev "NerdDinner.Identity" database (M10, DL-029) and
    // NerdDinner's Dinners-oriented TestDatabaseFixture, same
    // reasoning as the legacy test project's separate
    // IdentityTestDatabaseFixture/TestDatabaseFixture split: an
    // automated suite creating/dropping throwaway user accounts
    // shouldn't touch the same database a developer is manually
    // exercising the app against.
    public class IdentityTestDatabaseFixture : IDisposable
    {
        public const string ConnectionString =
            "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=NerdDinnerIdentityTests;Integrated Security=True;MultipleActiveResultSets=True";

        public IdentityTestDatabaseFixture()
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

    [CollectionDefinition("NerdDinner Identity LocalDB collection")]
    public class IdentityDatabaseCollection : ICollectionFixture<IdentityTestDatabaseFixture>
    {
    }
}
