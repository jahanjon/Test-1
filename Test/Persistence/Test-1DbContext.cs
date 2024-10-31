using FindJobs.DataAccess.Entities;
using FindJobs.DataAccess.Mapping;
using Microsoft.EntityFrameworkCore;

namespace FindJobs.DataAccess.Persistence
{
    public class FindJobsDbContext : DbContext
    {
        public DbSet<Applicant> Applicants { get; set; }
        public DbSet<Company> Companies { get; set; }
        public FindJobsDbContext(DbContextOptions<FindJobsDbContext> options) : base(options)
        {

            Database.Migrate();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var assembly = typeof(CountriesMapping).Assembly;
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
