﻿namespace WebApplication1.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class bodyParams : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.MockRequests", "BodyParameters", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.MockRequests", "BodyParameters");
        }
    }
}
