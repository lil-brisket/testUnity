using System.ComponentModel.DataAnnotations;
using NinjaMMORPG.Core.Enums;

namespace NinjaMMORPG.Core.Entities;

public class CombatAction
{
    public int Id { get; set; }
    public int BattleGridId { get; set; }
    public int CharacterId { get; set; }
    
    // Action details
    public ActionType Type { get; set; }
    public int APCost { get; set; }
    public int CPCost { get; set; } = 0;
    public int SPCost { get; set; } = 0;
    
    // Target information
    public int? TargetCharacterId { get; set; }
    public int? TargetRow { get; set; }
    public int? TargetColumn { get; set; }
    
    // Combat specifics
    public CombatType? CombatType { get; set; }
    public Element? Element { get; set; }
    public int? ItemId { get; set; }
    
    // Results
    public int? DamageDealt { get; set; }
    public int? HealingDone { get; set; }
    public bool IsSuccessful { get; set; } = false;
    
    // Timing
    public DateTime ActionTime { get; set; } = DateTime.UtcNow;
    public int TurnNumber { get; set; }
    
    // Navigation Properties
    public virtual BattleGrid BattleGrid { get; set; } = null!;
    public virtual Character Character { get; set; } = null!;
    public virtual Character? TargetCharacter { get; set; }
    
    // Validation methods
    public bool IsValidAction()
    {
        return Type switch
        {
            ActionType.Move => ValidateMovement(),
            ActionType.Attack => ValidateAttack(),
            ActionType.Jutsu => ValidateJutsu(),
            ActionType.Weapon => ValidateWeapon(),
            ActionType.Heal => ValidateHeal(),
            ActionType.Item => ValidateItem(),
            ActionType.Flee => ValidateFlee(),
            _ => false
        };
    }
    
    private bool ValidateMovement()
    {
        return TargetRow.HasValue && TargetColumn.HasValue && APCost > 0;
    }
    
    private bool ValidateAttack()
    {
        return TargetCharacterId.HasValue && APCost > 0;
    }
    
    private bool ValidateJutsu()
    {
        return TargetCharacterId.HasValue && APCost > 0 && CPCost > 0 && SPCost > 0;
    }
    
    private bool ValidateWeapon()
    {
        return TargetCharacterId.HasValue && APCost > 0;
    }
    
    private bool ValidateHeal()
    {
        return TargetCharacterId.HasValue && APCost > 0 && CPCost > 0 && SPCost > 0;
    }
    
    private bool ValidateItem()
    {
        return ItemId.HasValue && APCost > 0;
    }
    
    private bool ValidateFlee()
    {
        return APCost > 0;
    }
    
    public int CalculateDamage()
    {
        if (Type != ActionType.Attack && Type != ActionType.Jutsu && Type != ActionType.Weapon)
            return 0;
            
        var attacker = Character;
        var target = TargetCharacter;
        
        if (attacker == null || target == null)
            return 0;
            
        // Base attack calculation based on combat type
        int attackStat = CombatType switch
        {
            CombatType.Bukijutsu => (attacker.Bukijutsu * attacker.Strength) / 1000,
            CombatType.Ninjutsu => (attacker.Ninjutsu * attacker.Intelligence) / 1000,
            CombatType.Taijutsu => (attacker.Taijutsu * attacker.Speed) / 1000,
            CombatType.Genjutsu => (attacker.Genjutsu * attacker.Willpower) / 1000,
            _ => 0
        };
        
        // Base damage: Attack - Defense (minimum 1)
        int baseDamage = Math.Max(1, attackStat - target.DefenseValue);
        
        // Apply elemental modifiers if applicable
        if (Element.HasValue && target.Element.HasValue)
        {
            float elementalMultiplier = GetElementalMultiplier(Element.Value, target.Element.Value);
            baseDamage = (int)(baseDamage * elementalMultiplier);
        }
        
        return Math.Max(1, baseDamage);
    }
    
    private float GetElementalMultiplier(Element attackElement, Element defendElement)
    {
        return (attackElement, defendElement) switch
        {
            (Element.Water, Element.Fire) => 1.5f,
            (Element.Fire, Element.Earth) => 1.5f,
            (Element.Earth, Element.Water) => 1.5f,
            (Element.Fire, Element.Water) => 0.5f,
            (Element.Earth, Element.Fire) => 0.5f,
            (Element.Water, Element.Earth) => 0.5f,
            _ => 1.0f // No modifier for same elements or neutral
        };
    }
    
    public int CalculateHealing()
    {
        if (Type != ActionType.Heal)
            return 0;
            
        var healer = Character;
        if (healer == null)
            return 0;
            
        // Base healing based on medical ninja rank
        int baseHealing = healer.MedNinRank switch
        {
            MedNinRank.NoviceMedic => 50,
            MedNinRank.FieldMedic => 100,
            MedNinRank.MasterMedic => 200,
            MedNinRank.LegendaryHealer => 400,
            _ => 50
        };
        
        // Add intelligence bonus
        baseHealing += healer.Intelligence / 1000;
        
        // Consume resources
        healer.ConsumeResources(CPCost, SPCost);
        
        return baseHealing;
    }
}
