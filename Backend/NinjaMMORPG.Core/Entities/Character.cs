using System.ComponentModel.DataAnnotations;
using NinjaMMORPG.Core.Enums;

namespace NinjaMMORPG.Core.Entities;

public class Character
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    public Gender Gender { get; set; }
    public Village Village { get; set; }
    
    // Resource Stats
    public int HP { get; set; } = 100;
    public int MaxHP { get; set; } = 100;
    public int CP { get; set; } = 100;
    public int MaxCP { get; set; } = 100;
    public int SP { get; set; } = 100;
    public int MaxSP { get; set; } = 100;
    
    // Core Stats (Max: 250,000 each) - Start at 1
    public int Strength { get; set; } = 1;
    public int Intelligence { get; set; } = 1;
    public int Speed { get; set; } = 1;
    public int Willpower { get; set; } = 1;
    
    // Combat Stats (Max: 500,000 each) - Start at 1
    public int Ninjutsu { get; set; } = 1;
    public int Genjutsu { get; set; } = 1;
    public int Bukijutsu { get; set; } = 1;
    public int Taijutsu { get; set; } = 1;
    
    // Progression
    public Rank Rank { get; set; } = Rank.Student;
    public MedNinRank MedNinRank { get; set; } = MedNinRank.NoviceMedic;
    public int Level { get; set; } = 1;
    public long Experience { get; set; } = 0;
    public long MedNinExperience { get; set; } = 0;
    
    // Social and Economic
    public int? ClanId { get; set; }
    public int? SenseiId { get; set; }
    public int PocketMoney { get; set; } = 1000;
    public int Reputation { get; set; } = 0;
    public int Infamy { get; set; } = 0;
    
    // Location and Status
    public int CurrentLocationId { get; set; } = 1;
    public bool IsInBattle { get; set; } = false;
    public bool IsInHospital { get; set; } = false;
    public DateTime? HospitalEntryTime { get; set; }
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    
    // User Account
    public string UserId { get; set; } = string.Empty;
    
    // Navigation Properties
    public virtual Clan? Clan { get; set; }
    public virtual Character? Sensei { get; set; }
    public virtual List<Character> Students { get; set; } = new();
    public virtual List<Equipment> Equipment { get; set; } = new();
    public virtual BankAccount? BankAccount { get; set; }
    public virtual List<Mission> ActiveMissions { get; set; } = new();
    public virtual List<Mission> CompletedMissions { get; set; } = new();
    
    // Computed Properties
    public int DefenseValue => CalculateDefenseValue();
    public int CurrentAP { get; set; } = 100;
    public bool IsOutlaw => Village == Village.None;
    
    private int CalculateDefenseValue()
    {
        // Base defense from equipment and stats
        int baseDefense = Equipment?.Sum(e => e.DefenseBonus) ?? 0;
        
        // Add stat-based defense
        baseDefense += (Strength + Intelligence + Speed + Willpower) / 1000;
        
        return Math.Max(1, baseDefense);
    }
    
    public bool CanUseJutsu(int cpCost, int spCost)
    {
        return CP >= cpCost && SP >= spCost;
    }
    
    public void ConsumeResources(int cpCost, int spCost)
    {
        CP = Math.Max(0, CP - cpCost);
        SP = Math.Max(0, SP - spCost);
    }
    
    public void TakeDamage(int damage)
    {
        HP = Math.Max(0, HP - damage);
        if (HP == 0)
        {
            IsInHospital = true;
            HospitalEntryTime = DateTime.UtcNow;
        }
    }
    
    public void Heal(int amount)
    {
        HP = Math.Min(MaxHP, HP + amount);
        if (HP > 0 && IsInHospital)
        {
            IsInHospital = false;
            HospitalEntryTime = null;
        }
    }
    
    public bool CanAdvanceRank()
    {
        return Rank switch
        {
            Rank.Student => CanAdvanceToGenin(),
            Rank.Genin => CanAdvanceToChunin(),
            Rank.Chunin => CanAdvanceToJounin(),
            Rank.Jounin => CanAdvanceToSpecialJounin(),
            _ => false
        };
    }
    
    private bool CanAdvanceToGenin()
    {
        // Student → Genin: Complete Academy graduation + D-rank missions + AI battle victories
        return Level >= 5 && CompletedMissions.Count(m => m.Rank == MissionRank.D) >= 3;
    }
    
    private bool CanAdvanceToChunin()
    {
        // Genin → Chunin: Complete C-rank missions + combat requirement
        return Level >= 10 && CompletedMissions.Count(m => m.Rank == MissionRank.C) >= 5;
    }
    
    private bool CanAdvanceToJounin()
    {
        // Chunin → Jounin: Complete B-rank missions + PvP combat record
        return Level >= 20 && CompletedMissions.Count(m => m.Rank == MissionRank.B) >= 8;
    }
    
    private bool CanAdvanceToSpecialJounin()
    {
        // Jounin → Special Jounin: Complete A-rank missions + high PvP kill count + maxed offensive stat
        return Level >= 30 && 
               CompletedMissions.Count(m => m.Rank == MissionRank.A) >= 10 &&
               Math.Max(Ninjutsu, Math.Max(Genjutsu, Math.Max(Bukijutsu, Taijutsu))) >= 400000;
    }
}
