using Microsoft.EntityFrameworkCore;
using NerdDinner.Models;
using NetTopologySuite.Geometries;
using Xunit;

namespace NerdDinner.Tests.TestSupport
{
    // EF Core equivalent of NerdDinner.Tests' TestDatabaseFixture -- a
    // dedicated LocalDB catalog ("NerdDinnerTests", distinct from the
    // legacy suite's "NerdDinnerTests" and from the shared dev "NerdDinner"
    // database DL-028's app itself points at) created fresh and dropped
    // per test run, so this suite never touches real dev data. Unlike the
    // legacy test project, there's no VS-Test-Explorer AppDomain config
    // problem to work around here (DL-023 through DL-026) -- ASP.NET
    // Core's configuration system doesn't depend on ambient AppDomain
    // state, so a plain hardcoded connection string is fine.
    public class TestDatabaseFixture : IDisposable
    {
        public const string ConnectionString =
            "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=NerdDinnerTests;Integrated Security=True;MultipleActiveResultSets=True";

        public TestDatabaseFixture()
        {
            using var db = CreateContext();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            Seed(db);
        }

        public static NerdDinnerCoreContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<NerdDinnerCoreContext>()
                .UseLazyLoadingProxies()
                .UseSqlServer(ConnectionString, sql => sql.UseNetTopologySuite())
                .Options;
            return new NerdDinnerCoreContext(options);
        }

        // Wipes and re-seeds -- same reasoning as the legacy fixture's
        // Reset(): tests in this collection share one database for the
        // whole run, so mutating tests need every test to start from the
        // same known baseline regardless of run order.
        public void Reset()
        {
            using var db = CreateContext();
            db.RSVPs.RemoveRange(db.RSVPs);
            db.SaveChanges();
            db.Dinners.RemoveRange(db.Dinners);
            db.SaveChanges();
            Seed(db);
        }

        private static void Seed(NerdDinnerCoreContext db)
        {
            var seattle = new Point(-122.335167, 47.608013) { SRID = 4326 };

            var pastDinner = new Dinner
            {
                Title = "Past Dinner",
                EventDate = DateTime.Now.AddDays(-7),
                Description = "Already happened",
                HostedBy = "alice",
                ContactPhone = "555-0100",
                Address = "1 Past St",
                Country = "USA",
                Location = seattle
            };

            var futureDinnerHostedByAlice = new Dinner
            {
                Title = "Alice's Dinner",
                EventDate = DateTime.Now.AddDays(7),
                Description = "Upcoming, hosted by alice",
                HostedBy = "alice",
                ContactPhone = "555-0101",
                Address = "2 Future Ave",
                Country = "USA",
                Location = seattle
            };

            var futureDinnerHostedByBob = new Dinner
            {
                Title = "Bob's Dinner",
                EventDate = DateTime.Now.AddDays(14),
                Description = "Upcoming, hosted by bob",
                HostedBy = "bob",
                ContactPhone = "555-0102",
                Address = "3 Later Blvd",
                Country = "USA",
                Location = seattle,
                RSVPs = new List<RSVP>
                {
                    new RSVP { AttendeeName = "bob" },
                    new RSVP { AttendeeName = "carol" }
                }
            };

            // Far from Seattle -- outside FindByLocation's 2000m radius,
            // used to confirm the spatial distance query actually filters
            // rather than returning everything.
            var portlandDinner = new Dinner
            {
                Title = "Portland Dinner",
                EventDate = DateTime.Now.AddDays(10),
                Description = "Upcoming, hosted by dave, far from Seattle",
                HostedBy = "dave",
                ContactPhone = "555-0103",
                Address = "4 Rose City Way",
                Country = "USA",
                Location = new Point(-122.676483, 45.523064) { SRID = 4326 }
            };

            db.Dinners.Add(pastDinner);
            db.Dinners.Add(futureDinnerHostedByAlice);
            db.Dinners.Add(futureDinnerHostedByBob);
            db.Dinners.Add(portlandDinner);
            db.SaveChanges();
        }

        public void Dispose()
        {
            using var db = CreateContext();
            db.Database.EnsureDeleted();
        }
    }

    [CollectionDefinition("NerdDinner LocalDB collection")]
    public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
    {
    }
}
