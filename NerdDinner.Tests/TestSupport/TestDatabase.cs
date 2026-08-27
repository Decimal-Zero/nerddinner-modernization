using System;
using System.Data.Entity;
using System.Data.Spatial;
using System.Linq;
using NerdDinner.Models;
using Xunit;

namespace NerdDinner.Tests.TestSupport
{
    /// <summary>
    /// Creates a fresh NerdDinnerTests LocalDB database before the DB-backed
    /// test collection runs, and drops it afterward. Requires LocalDB
    /// (installed with Visual Studio) -- these tests do not run in an
    /// environment without it, including the sandbox this test project was
    /// authored in. See docs/02-plan/m2-characterization-tests.md for why.
    /// </summary>
    public class TestDatabaseFixture : IDisposable
    {
        public TestDatabaseFixture()
        {
            // Database.SetInitializer + a throwaway context access forces
            // EF to create the schema against the LocalDB connection string
            // in App.config. Deliberately does NOT reuse the app's own
            // aspnet-NerdDinner-*.mdf files checked into src/App_Data --
            // per the assessment's Category 4 finding, those are exactly
            // the kind of ad hoc, unreproducible data artifact this test
            // suite should not depend on.
            Database.SetInitializer(new DropCreateDatabaseAlways<NerdDinnerContext>());

            using (var db = new NerdDinnerContext())
            {
                db.Database.Initialize(force: true);
                Seed(db);
            }
        }

        /// <summary>
        /// Wipes and re-seeds the shared LocalDB database. Tests in this
        /// collection share one database for the whole run (schema
        /// creation is too slow to redo per-test), so any test that
        /// mutates data -- e.g. RSVPControllerTests.Register_* -- leaves
        /// state behind that can flip unrelated assertions elsewhere in
        /// the run (e.g. SearchControllerTests' RSVP-count ordering).
        /// Call this from each test class's constructor so every test
        /// starts from the same known baseline regardless of run order.
        /// </summary>
        public void Reset()
        {
            using (var db = new NerdDinnerContext())
            {
                // DbSet<T>.RemoveRange isn't available on EF 5.0 (the
                // version this app -- and this test project's
                // ProjectReference to it -- is pinned to); remove
                // entity-by-entity instead.
                foreach (var rsvp in db.RSVPs.ToList())
                {
                    db.RSVPs.Remove(rsvp);
                }
                db.SaveChanges();

                foreach (var dinner in db.Dinners.ToList())
                {
                    db.Dinners.Remove(dinner);
                }
                db.SaveChanges();

                Seed(db);
            }
        }

        private static void Seed(NerdDinnerContext db)
        {
            // JsonDinnerFromDinner (SearchController) dereferences
            // dinner.Location.Latitude/.Longitude unconditionally -- every
            // seeded dinner needs a real Location or SearchController
            // tests hit an unrelated NRE before exercising the behavior
            // they're meant to characterize. All three use the same point
            // (downtown Seattle); only relative distance/ordering matters
            // for the tests that use this fixture, not real-world accuracy.
            var seattle = DbGeography.FromText("POINT (-122.335167 47.608013)");

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
                Location = seattle
            };
            futureDinnerHostedByBob.RSVPs = new System.Collections.Generic.List<RSVP>
            {
                new RSVP { AttendeeName = "bob" },
                new RSVP { AttendeeName = "carol" }
            };

            db.Dinners.Add(pastDinner);
            db.Dinners.Add(futureDinnerHostedByAlice);
            db.Dinners.Add(futureDinnerHostedByBob);
            db.SaveChanges();
        }

        public void Dispose()
        {
            using (var db = new NerdDinnerContext())
            {
                db.Database.Delete();
            }
        }
    }

    [CollectionDefinition("NerdDinner LocalDB collection")]
    public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
    {
        // Marker class per xUnit convention -- no body needed. Grouping all
        // DB-backed tests under this collection means the fixture (and its
        // create/seed/drop lifecycle) runs once per test run, not once per
        // test class.
    }
}
