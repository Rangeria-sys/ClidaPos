using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Data
{
    public class ClidaposDbContext : DbContext
    {
        public DbSet<Registration> Registrations => Set<Registration>();
        public DbSet<WorkPeriodStart> WorkPeriodStarts => Set<WorkPeriodStart>();
        public DbSet<WorkPeriodEnd> WorkPeriodEnds => Set<WorkPeriodEnd>();

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var connectionString = config.GetConnectionString("ClidaDB");
            options.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Registration>(b =>
            {
                b.ToTable("Registration", "dbo");
                b.HasKey(x => x.UserID);
                b.Property(x => x.UserID).HasColumnType("nchar(100)");
                b.Property(x => x.UserType).HasColumnType("nchar(30)");
                b.Property(x => x.Password).HasColumnType("nchar(50)");
                b.Property(x => x.Name).HasColumnType("nchar(150)");
                b.Property(x => x.Active).HasColumnType("nchar(10)");
            });

            modelBuilder.Entity<WorkPeriodStart>(b =>
            {
                b.ToTable("WorkPeriodStart", "dbo");
                b.HasKey(x => x.ID);
                b.Property(x => x.WPStart).HasColumnType("datetime");
                b.Property(x => x.Status).HasColumnType("nchar(20)");
            });

            modelBuilder.Entity<WorkPeriodEnd>(b =>
            {
                b.ToTable("WorkPeriodEnd", "dbo");
                b.HasKey(x => x.Id);
                b.Property(x => x.WPEnd).HasColumnType("datetime");
            });
        }
    }
}