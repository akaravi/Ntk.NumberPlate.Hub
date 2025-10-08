using Microsoft.EntityFrameworkCore;
using Ntk.NumberPlate.Hub.Api.Models;

namespace Ntk.NumberPlate.Hub.Api.Data;

public class HubDbContext : DbContext
{
    public HubDbContext(DbContextOptions<HubDbContext> options) : base(options)
    {
    }

    public DbSet<VehicleDetectionRecord> VehicleDetections { get; set; }
    public DbSet<NodeInfo> Nodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<VehicleDetectionRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PlateNumber);
            entity.HasIndex(e => e.DetectionTime);
            entity.HasIndex(e => e.NodeId);
            entity.Property(e => e.PlateNumber).HasMaxLength(50);
            entity.Property(e => e.ImagePath).HasMaxLength(500);
        });

        modelBuilder.Entity<NodeInfo>(entity =>
        {
            entity.HasKey(e => e.NodeId);
            entity.Property(e => e.NodeId).HasMaxLength(100);
            entity.Property(e => e.NodeName).HasMaxLength(200);
        });
    }
}


