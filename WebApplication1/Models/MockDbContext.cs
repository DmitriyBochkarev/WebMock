using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace WebApplication1.Models
{
    public class MockDbContext : DbContext
    {
        public MockDbContext() : base("name=MockDbContext")
        {
        }

        public DbSet<MockRequest> MockRequests { get; set; }
        public DbSet<MockResponse> MockResponses { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MockRequest>()
                .HasRequired(r => r.Response)
                .WithMany(r => r.Requests)
                .HasForeignKey(r => r.MockResponseId);
        }
    }
}