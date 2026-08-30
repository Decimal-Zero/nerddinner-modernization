using Microsoft.EntityFrameworkCore;

namespace NerdDinner.Proxy.Models
{
    // EF Core replacement for the legacy app's EF6 NerdDinnerContext (M9,
    // decision-log.md DL-028). Points at the SAME physical "Dinners"/"RSVPs"
    // tables the legacy EF6 Migrations created (src/Migrations) -- this is
    // a strangler-fig cutover, not a data migration, so the schema is
    // reused as-is rather than re-created. Once Dinners/RSVP/Search are all
    // routed to this app, the legacy NerdDinnerContext (still present in
    // src/Models) is no longer touched by any live route -- left in place
    // rather than deleted, since M11 (decommission) is where the legacy
    // app itself goes away.
    public class NerdDinnerCoreContext : DbContext
    {
        public NerdDinnerCoreContext(DbContextOptions<NerdDinnerCoreContext> options)
            : base(options)
        {
        }

        public DbSet<Dinner> Dinners { get; set; }
        public DbSet<RSVP> RSVPs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Dinner>(entity =>
            {
                entity.ToTable("Dinners");
                entity.HasKey(d => d.DinnerID);
                entity.Property(d => d.Location).HasColumnType("geography");
                entity.Ignore(d => d.LocationDetail);
            });

            modelBuilder.Entity<RSVP>(entity =>
            {
                entity.ToTable("RSVPs");
                entity.HasKey(r => r.RsvpID);
                entity.HasOne(r => r.Dinner)
                    .WithMany(d => d.RSVPs)
                    .HasForeignKey(r => r.DinnerID)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
