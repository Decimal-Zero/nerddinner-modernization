namespace NerdDinner.Migrations
{
    using System.Data.Entity.Migrations;
    using System.Data.Entity.Spatial;
    using System.Linq;
    using NerdDinner.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<NerdDinnerContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        /// <summary>
        /// Placeholder sample data for a fresh database -- NOT a
        /// reproduction of the original checked-in .mdf's contents (that
        /// data was ad hoc and unreproducible, per the assessment's
        /// Category 4 finding; see decision-log.md DL-018). Clearly
        /// fictional, illustrative dinners only.
        /// </summary>
        protected override void Seed(NerdDinnerContext context)
        {
            if (context.Dinners.Any())
            {
                return;
            }

            var seattle = DbGeography.FromText("POINT (-122.335167 47.608013)");
            var portland = DbGeography.FromText("POINT (-122.676483 45.523064)");

            context.Dinners.AddOrUpdate(
                d => d.Title,
                new Dinner
                {
                    Title = "Sample Dinner: Seattle Nerds",
                    EventDate = System.DateTime.Now.AddDays(14),
                    Description = "A placeholder dinner seeded by EF6 Migrations -- not real data recovered from the original app.",
                    HostedBy = "sample-host",
                    ContactPhone = "555-0100",
                    Address = "400 Broad St, Seattle, WA 98109",
                    Country = "USA",
                    Location = seattle,
                },
                new Dinner
                {
                    Title = "Sample Dinner: Portland Meetup",
                    EventDate = System.DateTime.Now.AddDays(21),
                    Description = "A second placeholder dinner, purely for a fresh database to have something in it.",
                    HostedBy = "sample-host",
                    ContactPhone = "555-0101",
                    Address = "1945 SE Water Ave, Portland, OR 97214",
                    Country = "USA",
                    Location = portland,
                });

            context.SaveChanges();
        }
    }
}
