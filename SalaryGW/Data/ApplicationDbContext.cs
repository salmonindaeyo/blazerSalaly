using Microsoft.EntityFrameworkCore;
using SalaryGW.Models;

namespace SalaryGW.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SalaryPayment> SalaryPayments { get; set; }
    }
} 