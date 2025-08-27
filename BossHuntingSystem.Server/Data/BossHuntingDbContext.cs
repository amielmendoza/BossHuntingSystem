using Microsoft.EntityFrameworkCore;

namespace BossHuntingSystem.Server.Data
{
    public class BossHuntingDbContext : DbContext
    {
        public BossHuntingDbContext(DbContextOptions<BossHuntingDbContext> options) : base(options)
        {
        }
        
        public DbSet<Boss> Bosses { get; set; }
        public DbSet<BossDefeat> BossDefeats { get; set; }
        public DbSet<Member> Members { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure Boss entity
            modelBuilder.Entity<Boss>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.RespawnHours).IsRequired();
                entity.Property(e => e.LastKilledAt).IsRequired();
                
                // Configure the relationship
                entity.HasMany(e => e.Defeats)
                      .WithOne(e => e.Boss)
                      .HasForeignKey(e => e.BossId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Configure BossDefeat entity
            modelBuilder.Entity<BossDefeat>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BossId).IsRequired();
                entity.Property(e => e.BossName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DefeatedAtUtc).IsRequired(false); // Nullable
                entity.Property(e => e.LootsJson).HasColumnType("nvarchar(max)");
                entity.Property(e => e.AttendeesJson).HasColumnType("nvarchar(max)");
            });
            
            // Configure Member entity
            modelBuilder.Entity<Member>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CombatPower).IsRequired();
                entity.Property(e => e.GcashNumber).HasMaxLength(20);
                entity.Property(e => e.GcashName).HasMaxLength(100);
                entity.Property(e => e.CreatedAtUtc).IsRequired();
                entity.Property(e => e.UpdatedAtUtc).IsRequired();
                
                // Create unique index on Name to prevent duplicates
                entity.HasIndex(e => e.Name).IsUnique();
            });
            
            // Seed some initial data (optional)
            modelBuilder.Entity<Boss>().HasData(
                new Boss { Id = 1, Name = "Gadwa", RespawnHours = 1, LastKilledAt = new DateTime(2025, 8, 23, 10, 0, 0, DateTimeKind.Utc) }
            );
        }
    }
}
