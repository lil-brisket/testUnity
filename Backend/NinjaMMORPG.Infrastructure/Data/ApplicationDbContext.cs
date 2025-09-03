using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NinjaMMORPG.Core.Entities;

namespace NinjaMMORPG.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    // Game Entities
    public DbSet<Character> Characters { get; set; }
    public DbSet<Clan> Clans { get; set; }
    public DbSet<ClanApplication> ClanApplications { get; set; }
    public DbSet<Equipment> Equipment { get; set; }
    public DbSet<Mission> Missions { get; set; }
    public DbSet<BankAccount> BankAccounts { get; set; }
    public DbSet<BankTransaction> BankTransactions { get; set; }
    public DbSet<BattleGrid> BattleGrids { get; set; }
    public DbSet<BattleParticipant> BattleParticipants { get; set; }
    public DbSet<CombatAction> CombatActions { get; set; }
    public DbSet<Lottery> Lotteries { get; set; }
    public DbSet<LotteryTicket> LotteryTickets { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Character configurations
        modelBuilder.Entity<Character>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Strength).HasDefaultValue(1);
            entity.Property(e => e.Intelligence).HasDefaultValue(1);
            entity.Property(e => e.Speed).HasDefaultValue(1);
            entity.Property(e => e.Willpower).HasDefaultValue(1);
            entity.Property(e => e.Ninjutsu).HasDefaultValue(1);
            entity.Property(e => e.Genjutsu).HasDefaultValue(1);
            entity.Property(e => e.Bukijutsu).HasDefaultValue(1);
            entity.Property(e => e.Taijutsu).HasDefaultValue(1);
            entity.Property(e => e.HP).HasDefaultValue(100);
            entity.Property(e => e.MaxHP).HasDefaultValue(100);
            entity.Property(e => e.CP).HasDefaultValue(100);
            entity.Property(e => e.MaxCP).HasDefaultValue(100);
            entity.Property(e => e.SP).HasDefaultValue(100);
            entity.Property(e => e.MaxSP).HasDefaultValue(100);
            entity.Property(e => e.Level).HasDefaultValue(1);
            entity.Property(e => e.Rank).HasDefaultValue(Core.Enums.Rank.Student);
            entity.Property(e => e.MedNinRank).HasDefaultValue(Core.Enums.MedNinRank.NoviceMedic);
            entity.Property(e => e.PocketMoney).HasDefaultValue(1000);
            
            // Relationships
            entity.HasOne(e => e.Clan)
                  .WithMany(e => e.Members)
                  .HasForeignKey(e => e.ClanId)
                  .OnDelete(DeleteBehavior.SetNull);
                  
            entity.HasOne(e => e.Sensei)
                  .WithMany(e => e.Students)
                  .HasForeignKey(e => e.SenseiId)
                  .OnDelete(DeleteBehavior.SetNull);
                  
            entity.HasOne(e => e.BankAccount)
                  .WithOne(e => e.Character)
                  .HasForeignKey<BankAccount>(e => e.CharacterId);
        });
        
        // Clan configurations
        modelBuilder.Entity<Clan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.RyoInterestBonus).HasDefaultValue(0.05m);
            entity.Property(e => e.TrainingBonus).HasDefaultValue(0.10m);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
            
            // Relationships
            entity.HasOne(e => e.Shogun)
                  .WithMany()
                  .HasForeignKey(e => e.ShogunId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Equipment configurations
        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Value).HasDefaultValue(0);
            entity.Property(e => e.IsTradeable).HasDefaultValue(true);
        });
        
        // Mission configurations
        modelBuilder.Entity<Mission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsRepeatable).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });
        
        // Bank configurations
        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InterestRate).HasDefaultValue(0.02m);
            entity.Property(e => e.LastInterestCalculation).HasDefaultValueSql("GETUTCDATE()");
        });
        
        // Battle configurations
        modelBuilder.Entity<BattleGrid>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Rows).HasDefaultValue(5);
            entity.Property(e => e.Columns).HasDefaultValue(8);
            entity.Property(e => e.Status).HasDefaultValue(Core.Enums.BattleStatus.Preparing);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
        });
        
        // Lottery configurations
        modelBuilder.Entity<Lottery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TicketPrice).HasDefaultValue(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDrawn).HasDefaultValue(false);
        });
        
        // Configure many-to-many relationships
        modelBuilder.Entity<Character>()
            .HasMany(e => e.Equipment)
            .WithMany(e => e.EquippedBy)
            .UsingEntity(j => j.ToTable("CharacterEquipment"));
            
        modelBuilder.Entity<Character>()
            .HasMany(e => e.ActiveMissions)
            .WithMany(e => e.AssignedCharacters)
            .UsingEntity(j => j.ToTable("CharacterActiveMissions"));
            
        modelBuilder.Entity<Character>()
            .HasMany(e => e.CompletedMissions)
            .WithMany(e => e.CompletedBy)
            .UsingEntity(j => j.ToTable("CharacterCompletedMissions"));
    }
}
