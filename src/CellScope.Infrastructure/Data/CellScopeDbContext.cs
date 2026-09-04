using CellScope.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CellScope.Infrastructure.Data;

public class CellScopeDbContext : DbContext
{
    public CellScopeDbContext(DbContextOptions<CellScopeDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<CellularSnapshot> CellularSnapshots => Set<CellularSnapshot>();
    public DbSet<NeighborCell> NeighborCells => Set<NeighborCell>();
    public DbSet<CellObservation> CellObservations => Set<CellObservation>();
    public DbSet<TowerLocation> TowerLocations => Set<TowerLocation>();
    public DbSet<LocationPoint> LocationPoints => Set<LocationPoint>();
    public DbSet<SignalObservation> SignalObservations => Set<SignalObservation>();
    public DbSet<CellHandover> CellHandovers => Set<CellHandover>();
    public DbSet<LocalNetwork> LocalNetworks => Set<LocalNetwork>();
    public DbSet<NetworkDevice> NetworkDevices => Set<NetworkDevice>();
    public DbSet<CollectionSession> CollectionSessions => Set<CollectionSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQLite DateTimeOffset value conversion for native indexing & sorting
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?));
                foreach (var property in properties)
                {
                    modelBuilder.Entity(entityType.Name)
                        .Property(property.Name)
                        .HasConversion(new DateTimeOffsetToBinaryConverter());
                }
            }
        }

        // User
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            b.HasIndex(u => u.Username).IsUnique();
            b.HasIndex(u => u.Email).IsUnique();
            b.HasMany(u => u.Devices).WithOne().HasForeignKey(d => d.UserId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        });

        // UserSettings (Independent preference store)
        modelBuilder.Entity<UserSettings>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.UserId);
        });

        // Device
        modelBuilder.Entity<Device>(b =>
        {
            b.HasKey(d => d.Id);
            b.HasIndex(d => d.PairingCode);
            b.HasIndex(d => d.LastSeenAt);
            b.HasMany(d => d.Snapshots).WithOne().HasForeignKey(s => s.DeviceId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(d => d.Sessions).WithOne().HasForeignKey(s => s.DeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        // CellularSnapshot
        modelBuilder.Entity<CellularSnapshot>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.DeviceId);
            b.HasIndex(s => s.Timestamp);
            b.HasIndex(s => s.CellId);
            b.HasIndex(s => s.OperatorName);
            b.HasIndex(s => s.RadioTechnology);
            b.HasMany(s => s.NeighborCells).WithOne().HasForeignKey(n => n.SnapshotId).OnDelete(DeleteBehavior.Cascade);
        });

        // NeighborCell
        modelBuilder.Entity<NeighborCell>(b =>
        {
            b.HasKey(n => n.Id);
            b.HasIndex(n => n.SnapshotId);
            b.HasIndex(n => n.CellId);
        });

        // TowerLocation
        modelBuilder.Entity<TowerLocation>(b =>
        {
            b.HasKey(t => t.Id);
            b.HasIndex(t => t.CellId);
            b.HasIndex(t => new { t.Latitude, t.Longitude });
            b.HasIndex(t => t.OperatorName);
            b.HasIndex(t => t.RadioTechnology);
        });

        // LocationPoint
        modelBuilder.Entity<LocationPoint>(b =>
        {
            b.HasKey(l => l.Id);
            b.HasIndex(l => l.DeviceId);
            b.HasIndex(l => l.Timestamp);
        });

        // SignalObservation
        modelBuilder.Entity<SignalObservation>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.DeviceId);
            b.HasIndex(s => s.Timestamp);
        });

        // CellHandover
        modelBuilder.Entity<CellHandover>(b =>
        {
            b.HasKey(h => h.Id);
            b.HasIndex(h => h.DeviceId);
            b.HasIndex(h => h.Timestamp);
        });

        // LocalNetwork & NetworkDevice
        modelBuilder.Entity<LocalNetwork>(b =>
        {
            b.HasKey(n => n.Id);
            b.HasIndex(n => n.ScannedAt);
            b.HasMany(n => n.Devices).WithOne().HasForeignKey(d => d.LocalNetworkId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NetworkDevice>(b =>
        {
            b.HasKey(d => d.Id);
            b.HasIndex(d => d.LocalNetworkId);
            b.HasIndex(d => d.IpAddress);
        });
    }
}
