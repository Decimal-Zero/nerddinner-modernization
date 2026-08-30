using System.Data.Entity;

namespace NerdDinner.Models
{
    public class NerdDinnerContext : DbContext
    {
        // You can add custom code to this file. Changes will not be overwritten.
        // 
        // If you want Entity Framework to drop and regenerate your database
        // automatically whenever you change your model schema, add the following
        // code to the Application_Start method in your Global.asax file.
        // Note: this will destroy and re-create your database with every model change.
        // 
        // System.Data.Entity.Database.SetInitializer(new System.Data.Entity.DropCreateDatabaseIfModelChanges<NerdDinner.Models.NerdDinnerContext>());

        public NerdDinnerContext() : base("name=NerdDinnerContext")
        {
        }

        // Lets a caller supply a raw connection string directly, bypassing
        // ConfigurationManager's name-based ("name=NerdDinnerContext")
        // config lookup entirely. The running app never needs this -- it
        // always uses the parameterless constructor above -- but
        // NerdDinner.Tests does, to work around a VS Test Explorer AppDomain
        // config-resolution issue unrelated to this class. See
        // decision-log.md DL-023/DL-024.
        public NerdDinnerContext(string connectionString) : base(connectionString)
        {
        }

        public DbSet<Dinner> Dinners { get; set; }
        public DbSet<RSVP> RSVPs { get; set; }
    }
}
