using System.ComponentModel.DataAnnotations;
using NinjaMMORPG.Core.Enums;

namespace NinjaMMORPG.Core.Entities;

public class Equipment
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    public EquipmentSlot Slot { get; set; }
    public EquipmentType Type { get; set; }
    public Element Element { get; set; } = Element.None;
    
    // Stat Bonuses
    public int StrengthBonus { get; set; } = 0;
    public int IntelligenceBonus { get; set; } = 0;
    public int SpeedBonus { get; set; } = 0;
    public int WillpowerBonus { get; set; } = 0;
    
    public int NinjutsuBonus { get; set; } = 0;
    public int GenjutsuBonus { get; set; } = 0;
    public int BukijutsuBonus { get; set; } = 0;
    public int TaijutsuBonus { get; set; } = 0;
    
    public int HPBonus { get; set; } = 0;
    public int CPBonus { get; set; } = 0;
    public int SPBonus { get; set; } = 0;
    public int DefenseBonus { get; set; } = 0;
    
    // Requirements
    public int MinLevel { get; set; } = 1;
    public Rank MinRank { get; set; } = Rank.Student;
    
    // Economic
    public int Value { get; set; } = 0;
    public bool IsTradeable { get; set; } = true;
    
    // Navigation Properties
    public virtual List<Character> EquippedBy { get; set; } = new();
    
    // Computed Properties
    public int TotalStatBonus => StrengthBonus + IntelligenceBonus + SpeedBonus + WillpowerBonus;
    public int TotalCombatBonus => NinjutsuBonus + GenjutsuBonus + BukijutsuBonus + TaijutsuBonus;
}

public enum EquipmentSlot
{
    Head = 1,           // Headband, mask, helmet
    Body = 2,           // Chest armor, robes
    ArmsLeft = 3,       // Left arm guards, gauntlets
    ArmsRight = 4,      // Right arm guards, gauntlets
    Legs = 5,           // Leg armor, protective pants
    Feet = 6            // Boots, sandals, speed gear
}

public enum EquipmentType
{
    Weapon = 1,         // Swords, kunai, shuriken
    Armor = 2,          // Protective gear
    Accessory = 3,      // Rings, necklaces, etc.
    Tool = 4            // Utility items
}
