namespace WebApplication1.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class query : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.MockRequests", "QueryParameters", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.MockRequests", "QueryParameters");
        }
    }
}
