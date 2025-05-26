namespace WebApplication1.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MockRequests",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Path = c.String(),
                        Method = c.String(),
                        MockResponseId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.MockResponses", t => t.MockResponseId, cascadeDelete: true)
                .Index(t => t.MockResponseId);
            
            CreateTable(
                "dbo.MockResponses",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StatusCode = c.Int(nullable: false),
                        Body = c.String(),
                        HeadersJson = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.MockRequests", "MockResponseId", "dbo.MockResponses");
            DropIndex("dbo.MockRequests", new[] { "MockResponseId" });
            DropTable("dbo.MockResponses");
            DropTable("dbo.MockRequests");
        }
    }
}
