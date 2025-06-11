using Microsoft.EntityFrameworkCore;
using SalaryProcessor.Models;

namespace SalaryProcessor.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SalaryPayment> SalaryPayments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SalaryPayment>()
                .HasIndex(s => s.TransactionId)
                .IsUnique();
        }
    }
} 