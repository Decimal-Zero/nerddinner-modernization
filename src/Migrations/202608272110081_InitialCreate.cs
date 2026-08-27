namespace NerdDinner.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Dinners",
                c => new
                    {
                        DinnerID = c.Int(nullable: false, identity: true),
                        Title = c.String(nullable: false, maxLength: 50),
                        EventDate = c.DateTime(nullable: false),
                        Description = c.String(nullable: false, maxLength: 256),
                        HostedBy = c.String(maxLength: 20),
                        ContactPhone = c.String(nullable: false, maxLength: 20),
                        Address = c.String(nullable: false, maxLength: 50),
                        Country = c.String(),
                        Location = c.Geography(),
                    })
                .PrimaryKey(t => t.DinnerID);
            
            CreateTable(
                "dbo.RSVPs",
                c => new
                    {
                        RsvpID = c.Int(nullable: false, identity: true),
                        DinnerID = c.Int(nullable: false),
                        AttendeeName = c.String(),
                    })
                .PrimaryKey(t => t.RsvpID)
                .ForeignKey("dbo.Dinners", t => t.DinnerID, cascadeDelete: true)
                .Index(t => t.DinnerID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RSVPs", "DinnerID", "dbo.Dinners");
            DropIndex("dbo.RSVPs", new[] { "DinnerID" });
            DropTable("dbo.RSVPs");
            DropTable("dbo.Dinners");
        }
    }
}
