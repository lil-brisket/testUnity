using System.ComponentModel.DataAnnotations;
using NinjaMMORPG.Core.Enums;

namespace NinjaMMORPG.Core.Entities;

public class Mission
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    public MissionRank Rank { get; set; }
    public Village Village { get; set; }
    
    // Requirements
    public int MinLevel { get; set; }
    public Rank MinRank { get; set; }
    
    // Rewards
    public int ExperienceReward { get; set; }
    public int MoneyReward { get; set; }
    public int StatReward { get; set; }
    
    // Mission Type
    public MissionType Type { get; set; }
    public bool IsRepeatable { get; set; } = false;
    public bool IsActive { get; set; } = true;
    
    // Time Limits
    public int? TimeLimitMinutes { get; set; }
    public DateTime? ExpiryDate { get; set; }
    
    // Navigation Properties
    public virtual List<Character> AssignedCharacters { get; set; } = new();
    public virtual List<Character> CompletedBy { get; set; } = new();
}

public enum MissionType
{
    Combat = 1,         // Defeat enemies
    Delivery = 2,       // Transport items
    Escort = 3,         // Protect NPCs
    Investigation = 4,  // Gather information
    Training = 5,       // Improve stats
    Special = 6         // Unique events
}
