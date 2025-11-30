using Microsoft.EntityFrameworkCore;
using PermissionSystemApi.Models;

namespace PermissionSystemApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<InvoiceQRequest> InvoiceQRequests { get; set; }
        public DbSet<OcityRequest> OcityRequests { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ApprovalLog> ApprovalLogs { get; set; }
        public DbSet<PeriodicReview> PeriodicReviews { get; set; }
        public DbSet<PermissionRecord> PermissionRecords { get; set; }
        public DbSet<ManagerAssignment> ManagerAssignments { get; set; }
        public DbSet<RequestActionHistory> RequestActionHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Fluent API relationships
            modelBuilder.Entity<ApprovalLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PermissionRecord>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }

    }
}
