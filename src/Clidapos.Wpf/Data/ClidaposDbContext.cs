using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Data
{
    public class ClidaposDbContext : DbContext
    {
        public DbSet<Registration> Registrations => Set<Registration>();

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
        }
    }
}