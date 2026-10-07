using Microsoft.EntityFrameworkCore;
using SolarGenerationApi.Models;

namespace SolarGenerationApi.Data;

public class SolarGenerationDbContext : DbContext
{
    public SolarGenerationDbContext(DbContextOptions<SolarGenerationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Province> Provinces => Set<Province>();

    public DbSet<District> Districts => Set<District>();

    public DbSet<GridSubstation> GridSubstations => Set<GridSubstation>();

    public DbSet<SolarInstallation> SolarInstallations => Set<SolarInstallation>();

    public DbSet<GenerationReading> GenerationReadings => Set<GenerationReading>();

    public DbSet<User> Users => Set<User>();
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Province>()
            .HasMany(p => p.Districts)
            .WithOne(d => d.Province)
            .HasForeignKey(d => d.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<District>()
            .HasMany(d => d.GridSubstations)
            .WithOne(g => g.District)
            .HasForeignKey(g => g.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GridSubstation>()
            .HasMany(g => g.SolarInstallations)
            .WithOne(s => s.GridSubstation)
            .HasForeignKey(s => s.GridSubstationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SolarInstallation>()
            .HasMany(s => s.GenerationReadings)
            .WithOne(r => r.SolarInstallation)
            .HasForeignKey(r => r.SolarInstallationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SolarInstallation>()
            .HasIndex(s => s.MeterIdentifier)
            .IsUnique();

        modelBuilder.Entity<GridSubstation>()
            .HasIndex(g => g.Code)
            .IsUnique();

        modelBuilder.Entity<GenerationReading>()
            .HasIndex(r => new { r.SolarInstallationId, r.Timestamp })
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(u => u.Province)
            .WithMany()
            .HasForeignKey(u => u.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne(u => u.District)
            .WithMany()
            .HasForeignKey(u => u.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}