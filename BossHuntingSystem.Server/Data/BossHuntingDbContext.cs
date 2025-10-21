using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace BossHuntingSystem.Server.Data
{
    public class BossHuntingDbContext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public BossHuntingDbContext(DbContextOptions<BossHuntingDbContext> options) : base(options)
        {
        }

        public BossHuntingDbContext(DbContextOptions<BossHuntingDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private int? GetCurrentClientId()
        {
            // Get ClientId from HttpContext if available
            if (_httpContextAccessor?.HttpContext?.Items.TryGetValue("ClientId", out var clientId) == true)
            {
                return clientId as int?;
            }
            return null;
        }

        private bool IsSuperAdmin()
        {
            if (_httpContextAccessor?.HttpContext?.Items.TryGetValue("UserRole", out var role) == true)
            {
                return (role as string) == "SuperAdmin";
            }
            return false;
        }

        public DbSet<Boss> Bosses { get; set; }
        public DbSet<BossDefeat> BossDefeats { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Client entity
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.LicenseKey).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LicenseExpirationDate).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.ContactEmail).IsRequired().HasMaxLength(200);

                // Create unique index on LicenseKey
                entity.HasIndex(e => e.LicenseKey).IsUnique();

                // Configure relationships
                entity.HasMany(e => e.Members)
                      .WithOne(e => e.Client)
                      .HasForeignKey(e => e.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Bosses)
                      .WithOne(e => e.Client)
                      .HasForeignKey(e => e.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.BossDefeats)
                      .WithOne(e => e.Client)
                      .HasForeignKey(e => e.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Users)
                      .WithOne(e => e.Client)
                      .HasForeignKey(e => e.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.AuditLogs)
                      .WithOne(e => e.Client)
                      .HasForeignKey(e => e.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);

                // Create unique index on Username
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                // Create composite index for ClientId and Username
                entity.HasIndex(e => new { e.ClientId, e.Username });
            });

            // Configure AuditLog entity
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);

                // Create index for querying
                entity.HasIndex(e => new { e.ClientId, e.Timestamp });
                entity.HasIndex(e => new { e.ClientId, e.EntityType, e.EntityId });
            });

            // Configure Boss entity
            modelBuilder.Entity<Boss>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.RespawnHours).IsRequired();
                entity.Property(e => e.LastKilledAt).IsRequired();
                entity.Property(e => e.ClientId).IsRequired();

                // Configure the relationship
                entity.HasMany(e => e.Defeats)
                      .WithOne(e => e.Boss)
                      .HasForeignKey(e => e.BossId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Create composite index for ClientId and Name
                entity.HasIndex(e => new { e.ClientId, e.Name });
            });

            // Configure BossDefeat entity
            modelBuilder.Entity<BossDefeat>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BossId).IsRequired();
                entity.Property(e => e.ClientId).IsRequired();
                entity.Property(e => e.BossName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DefeatedAtUtc).IsRequired(false); // Nullable
                entity.Property(e => e.LootsJson).HasColumnType("nvarchar(max)");
                entity.Property(e => e.AttendeesJson).HasColumnType("nvarchar(max)");

                // Create index for querying
                entity.HasIndex(e => new { e.ClientId, e.DefeatedAtUtc });
            });

            // Configure Member entity
            modelBuilder.Entity<Member>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CombatPower).IsRequired();
                entity.Property(e => e.ClientId).IsRequired();
                entity.Property(e => e.GcashNumber).HasMaxLength(20);
                entity.Property(e => e.GcashName).HasMaxLength(100);
                entity.Property(e => e.CreatedAtUtc).IsRequired();
                entity.Property(e => e.UpdatedAtUtc).IsRequired();

                // Create unique index on ClientId and Name to prevent duplicates within a client
                entity.HasIndex(e => new { e.ClientId, e.Name }).IsUnique();
            });

            // Apply global query filters for tenant isolation
            // SuperAdmin can see all data, others only see their client's data
            var currentClientId = GetCurrentClientId();
            var isSuperAdmin = IsSuperAdmin();

            if (currentClientId.HasValue && !isSuperAdmin)
            {
                modelBuilder.Entity<Boss>().HasQueryFilter(e => e.ClientId == currentClientId.Value);
                modelBuilder.Entity<BossDefeat>().HasQueryFilter(e => e.ClientId == currentClientId.Value);
                modelBuilder.Entity<Member>().HasQueryFilter(e => e.ClientId == currentClientId.Value);
                modelBuilder.Entity<User>().HasQueryFilter(e => e.ClientId == currentClientId.Value);
                modelBuilder.Entity<AuditLog>().HasQueryFilter(e => e.ClientId == currentClientId.Value);
            }
        }
    }
}
