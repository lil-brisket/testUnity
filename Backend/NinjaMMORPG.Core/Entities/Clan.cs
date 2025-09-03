using System.ComponentModel.DataAnnotations;
using NinjaMMORPG.Core.Enums;

namespace NinjaMMORPG.Core.Entities;

public class Clan
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;
    
    public Village Village { get; set; }
    
    // Leadership Structure
    public int ShogunId { get; set; }  // Clan leader
    public virtual Character Shogun { get; set; } = null!;
    
    // Benefits
    public decimal RyoInterestBonus { get; set; } = 0.05m;  // 5% bonus
    public decimal TrainingBonus { get; set; } = 0.10m;     // 10% bonus
    
    // Creation and Management
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public virtual List<Character> Advisors { get; set; } = new();
    public virtual List<Character> Members { get; set; } = new();
    public virtual List<ClanApplication> PendingApplications { get; set; } = new();
    
    // Computed Properties
    public int TotalMembers => Members.Count + Advisors.Count + 1; // +1 for Shogun
    public bool IsAtCapacity => TotalMembers >= 50; // Maximum clan size
    
    public bool CanAddMember()
    {
        return IsActive && !IsAtCapacity;
    }
    
    public bool IsLeadership(Character character)
    {
        return character.Id == ShogunId || 
               Advisors.Any(a => a.Id == character.Id);
    }
    
    public void AddAdvisor(Character character)
    {
        if (Advisors.Count < 2 && !IsLeadership(character))
        {
            Advisors.Add(character);
        }
    }
    
    public void RemoveAdvisor(Character character)
    {
        Advisors.RemoveAll(a => a.Id == character.Id);
    }
}

public class ClanApplication
{
    public int Id { get; set; }
    public int CharacterId { get; set; }
    public int ClanId { get; set; }
    
    [StringLength(500)]
    public string ApplicationMessage { get; set; } = string.Empty;
    
    public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    
    // Navigation Properties
    public virtual Character Character { get; set; } = null!;
    public virtual Clan Clan { get; set; } = null!;
}

public enum ApplicationStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3
}
