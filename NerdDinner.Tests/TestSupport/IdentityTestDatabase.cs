using System;
using System.Data.Entity;
using NerdDinner.Models;
using Xunit;

namespace NerdDinner.Tests.TestSupport
{
    /// <summary>
    /// Creates a fresh NerdDinnerIdentityTests LocalDB database (ASP.NET
    /// Identity's AspNetUsers/AspNetRoles/etc. tables) before the
    /// Identity-backed test collection runs, and drops it afterward. Kept
    /// as a separate database from TestDatabaseFixture's NerdDinnerTests
    /// (see DefaultConnection vs. NerdDinnerContext in App.config) so the
    /// two fixtures' drop/create lifecycles can't interfere with each
    /// other.
    /// </summary>
    public class IdentityTestDatabaseFixture : IDisposable
    {
        public IdentityTestDatabaseFixture()
        {
            Database.SetInitializer(new DropCreateDatabaseAlways<ApplicationDbContext>());

            using (var db = new ApplicationDbContext(TestConnectionStrings.Get("DefaultConnection")))
            {
                db.Database.Initialize(force: true);
            }
        }

        public void Dispose()
        {
            using (var db = new ApplicationDbContext(TestConnectionStrings.Get("DefaultConnection")))
            {
                db.Database.Delete();
            }
        }
    }

    [CollectionDefinition("NerdDinner Identity LocalDB collection")]
    public class IdentityDatabaseCollection : ICollectionFixture<IdentityTestDatabaseFixture>
    {
        // Marker class per xUnit convention -- no body needed.
    }
}
