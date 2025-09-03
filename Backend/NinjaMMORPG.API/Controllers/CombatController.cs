using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using NinjaMMORPG.API.Hubs;
using NinjaMMORPG.Core.Entities;
using NinjaMMORPG.Core.Enums;
using NinjaMMORPG.Infrastructure.Data;
using System.Security.Claims;

namespace NinjaMMORPG.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CombatController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CombatController> _logger;
    private readonly IHubContext<CombatHub> _combatHub;
    
    public CombatController(ApplicationDbContext context, ILogger<CombatController> logger, IHubContext<CombatHub> combatHub)
    {
        _context = context;
        _logger = logger;
        _combatHub = combatHub;
    }
    
    // POST: api/Combat/create-battle
    [HttpPost("create-battle")]
    public async Task<ActionResult<BattleGrid>> CreateBattle(CreateBattleRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var character = await _context.Characters
            .FirstOrDefaultAsync(c => c.Id == request.CharacterId && c.UserId == userId);
            
        if (character == null)
            return NotFound("Character not found");
            
        if (character.IsInBattle)
            return BadRequest("Character is already in battle");
            
        // Create new battle
        var battle = new BattleGrid
        {
            Type = request.BattleType,
            MissionId = request.MissionId,
            Status = BattleStatus.Preparing,
            CreatedDate = DateTime.UtcNow
        };
        
        _context.BattleGrids.Add(battle);
        await _context.SaveChangesAsync();
        
        // Add character as first participant
        var participant = new BattleParticipant
        {
            BattleGridId = battle.Id,
            CharacterId = character.Id,
            CurrentRow = 0,
            CurrentColumn = 0,
            CurrentHP = character.HP,
            Initiative = character.Speed,
            LastActionTime = DateTime.UtcNow
        };
        
        _context.BattleParticipants.Add(participant);
        character.IsInBattle = true;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Battle {BattleId} created by character {CharacterId}", battle.Id, character.Id);
        
        return Ok(battle);
    }
    
    // POST: api/Combat/join-battle
    [HttpPost("join-battle")]
    public async Task<ActionResult> JoinBattle(JoinBattleRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var character = await _context.Characters
            .FirstOrDefaultAsync(c => c.Id == request.CharacterId && c.UserId == userId);
            
        if (character == null)
            return NotFound("Character not found");
            
        if (character.IsInBattle)
            return BadRequest("Character is already in battle");
            
        var battle = await _context.BattleGrids
            .Include(b => b.Participants)
            .FirstOrDefaultAsync(b => b.Id == request.BattleId);
            
        if (battle == null)
            return NotFound("Battle not found");
            
        if (battle.Status != BattleStatus.Preparing)
            return BadRequest("Battle is no longer accepting participants");
            
        // Find available starting position
        var (startRow, startCol) = FindAvailablePosition(battle);
        
        var participant = new BattleParticipant
        {
            BattleGridId = battle.Id,
            CharacterId = character.Id,
            CurrentRow = startRow,
            CurrentColumn = startCol,
            CurrentHP = character.HP,
            Initiative = character.Speed,
            LastActionTime = DateTime.UtcNow
        };
        
        _context.BattleParticipants.Add(participant);
        character.IsInBattle = true;
        
        await _context.SaveChangesAsync();
        
        // Notify other participants via SignalR
        await _combatHub.Clients.Group($"Battle_{battle.Id}").SendAsync("PlayerJoinedBattle", new
        {
            CharacterId = character.Id,
            CharacterName = character.Name,
            Row = startRow,
            Column = startCol
        });
        
        _logger.LogInformation("Character {CharacterId} joined battle {BattleId}", character.Id, battle.Id);
        
        return Ok(new { BattleId = battle.Id, StartRow = startRow, StartColumn = startCol });
    }
    
    // POST: api/Combat/action
    [HttpPost("action")]
    [EnableRateLimiting("CombatActions")]
    public async Task<ActionResult<CombatActionResult>> ProcessCombatAction(CombatActionRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var character = await _context.Characters
            .FirstOrDefaultAsync(c => c.Id == request.CharacterId && c.UserId == userId);
            
        if (character == null)
            return NotFound("Character not found");
            
        var battle = await _context.BattleGrids
            .Include(b => b.Participants)
            .ThenInclude(p => p.Character)
            .FirstOrDefaultAsync(b => b.Id == request.BattleId);
            
        if (battle == null)
            return NotFound("Battle not found");
            
        if (battle.Status != BattleStatus.InProgress)
            return BadRequest("Battle is not in progress");
            
        var participant = battle.Participants.FirstOrDefault(p => p.CharacterId == character.Id);
        if (participant == null)
            return BadRequest("Character is not participating in this battle");
            
        if (participant.IsDefeated)
            return BadRequest("Character is defeated and cannot act");
            
        // Validate action
        if (!ValidateCombatAction(request, participant, battle))
            return BadRequest("Invalid combat action");
            
        // Process action
        var result = await ProcessAction(request, participant, battle);
        
        // Save to database
        var combatAction = new CombatAction
        {
            BattleGridId = battle.Id,
            CharacterId = character.Id,
            Type = request.ActionType,
            APCost = request.APCost,
            CPCost = request.CPCost,
            SPCost = request.SPCost,
            TargetCharacterId = request.TargetCharacterId,
            TargetRow = request.TargetRow,
            TargetColumn = request.TargetColumn,
            CombatType = request.CombatType,
            Element = request.Element,
            ItemId = request.ItemId,
            DamageDealt = result.DamageDealt,
            HealingDone = result.HealingDone,
            IsSuccessful = result.IsSuccessful,
            TurnNumber = battle.Participants.Count(p => !p.IsDefeated),
            ActionTime = DateTime.UtcNow
        };
        
        _context.CombatActions.Add(combatAction);
        await _context.SaveChangesAsync();
        
        // Broadcast action to all battle participants via SignalR
        await _combatHub.Clients.Group($"Battle_{battle.Id}").SendAsync("CombatActionProcessed", new
        {
            ActionId = combatAction.Id,
            CharacterId = character.Id,
            CharacterName = character.Name,
            ActionType = request.ActionType,
            TargetId = request.TargetCharacterId,
            TargetRow = request.TargetRow,
            TargetColumn = request.TargetColumn,
            DamageDealt = result.DamageDealt,
            HealingDone = result.HealingDone,
            IsSuccessful = result.IsSuccessful,
            Timestamp = DateTime.UtcNow
        });
        
        _logger.LogInformation("Combat action processed: {ActionType} by {CharacterId} in battle {BattleId}", 
            request.ActionType, character.Id, battle.Id);
        
        return Ok(result);
    }
    
    // POST: api/Combat/start-battle
    [HttpPost("start-battle")]
    public async Task<ActionResult> StartBattle(int battleId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var battle = await _context.BattleGrids
            .Include(b => b.Participants)
            .FirstOrDefaultAsync(b => b.Id == battleId);
            
        if (battle == null)
            return NotFound("Battle not found");
            
        if (battle.Status != BattleStatus.Preparing)
            return BadRequest("Battle cannot be started");
            
        if (battle.Participants.Count < 1)
            return BadRequest("Battle needs at least one participant");
            
        // Start battle
        battle.StartBattle();
        
        // Set initiative order based on Speed stat
        var orderedParticipants = battle.Participants.OrderByDescending(p => p.Initiative).ToList();
        for (int i = 0; i < orderedParticipants.Count; i++)
        {
            orderedParticipants[i].Initiative = i + 1;
        }
        
        await _context.SaveChangesAsync();
        
        // Notify participants via SignalR
        await _combatHub.Clients.Group($"Battle_{battle.Id}").SendAsync("BattleStarted", new
        {
            BattleId = battle.Id,
            Participants = battle.Participants.Select(p => new
            {
                CharacterId = p.CharacterId,
                Row = p.CurrentRow,
                Column = p.CurrentColumn,
                Initiative = p.Initiative
            }).ToList()
        });
        
        _logger.LogInformation("Battle {BattleId} started with {ParticipantCount} participants", 
            battle.Id, battle.Participants.Count);
        
        return Ok(new { BattleId = battle.Id, Status = battle.Status });
    }
    
    // POST: api/Combat/end-turn
    [HttpPost("end-turn")]
    public async Task<ActionResult> EndTurn(int battleId, int characterId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var character = await _context.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId && c.UserId == userId);
            
        if (character == null)
            return NotFound("Character not found");
            
        var battle = await _context.BattleGrids
            .Include(b => b.Participants)
            .FirstOrDefaultAsync(b => b.Id == battleId);
            
        if (battle == null)
            return NotFound("Battle not found");
            
        var participant = battle.Participants.FirstOrDefault(p => p.CharacterId == characterId);
        if (participant == null)
            return BadRequest("Character is not participating in this battle");
            
        // Reset AP for next turn
        participant.CurrentAP = 100;
        participant.LastActionTime = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        // Notify other participants via SignalR
        await _combatHub.Clients.Group($"Battle_{battle.Id}").SendAsync("TurnEnded", new
        {
            CharacterId = characterId,
            CharacterName = character.Name,
            NewAP = 100,
            Timestamp = DateTime.UtcNow
        });
        
        _logger.LogInformation("Character {CharacterId} ended turn in battle {BattleId}", characterId, battle.Id);
        
        return Ok(new { CharacterId = characterId, NewAP = 100 });
    }
    
    // Helper methods
    private (int row, int col) FindAvailablePosition(BattleGrid battle)
    {
        // Simple algorithm to find available position
        for (int row = 0; row < battle.Rows; row++)
        {
            for (int col = 0; col < battle.Columns; col++)
            {
                if (!battle.IsPositionOccupied(row, col))
                    return (row, col);
            }
        }
        
        // Fallback to first available position
        return (0, 0);
    }
    
    private bool ValidateCombatAction(CombatActionRequest request, BattleParticipant participant, BattleGrid battle)
    {
        // Check AP availability
        if (participant.CurrentAP < request.APCost)
            return false;
            
        // Check if character has enough CP/SP for jutsu
        if (request.ActionType == ActionType.Jutsu || request.ActionType == ActionType.Heal)
        {
            var character = participant.Character;
            if (!character.CanUseJutsu(request.CPCost, request.SPCost))
                return false;
        }
        
        // Validate movement
        if (request.ActionType == ActionType.Move)
        {
            if (!battle.IsValidPosition(request.TargetRow!.Value, request.TargetColumn!.Value))
                return false;
                
            if (battle.IsPositionOccupied(request.TargetRow.Value, request.TargetColumn.Value))
                return false;
                
            var movementCost = battle.CalculateMovementCost(participant.CurrentRow, participant.CurrentColumn, 
                request.TargetRow.Value, request.TargetColumn.Value);
                
            if (movementCost != request.APCost)
                return false;
        }
        
        return true;
    }
    
    private async Task<CombatActionResult> ProcessAction(CombatActionRequest request, BattleParticipant participant, BattleGrid battle)
    {
        var result = new CombatActionResult { IsSuccessful = true };
        
        // Consume AP
        participant.ConsumeAP(request.APCost);
        
        // Consume CP/SP for jutsu
        if (request.ActionType == ActionType.Jutsu || request.ActionType == ActionType.Heal)
        {
            participant.Character.ConsumeResources(request.CPCost, request.SPCost);
        }
        
        // Process specific action types
        switch (request.ActionType)
        {
            case ActionType.Move:
                participant.MoveTo(request.TargetRow!.Value, request.TargetColumn!.Value);
                break;
                
            case ActionType.Attack:
            case ActionType.Jutsu:
            case ActionType.Weapon:
                result.DamageDealt = CalculateDamage(participant, request, battle);
                ApplyDamage(result.DamageDealt, request.TargetCharacterId!.Value, battle);
                break;
                
            case ActionType.Heal:
                result.HealingDone = CalculateHealing(participant, request);
                ApplyHealing(result.HealingDone, request.TargetCharacterId!.Value, battle);
                break;
        }
        
        await _context.SaveChangesAsync();
        return result;
    }
    
    private int CalculateDamage(BattleParticipant attacker, CombatActionRequest request, BattleGrid battle)
    {
        var target = battle.Participants.FirstOrDefault(p => p.CharacterId == request.TargetCharacterId);
        if (target == null) return 0;
        
        var attackStat = request.CombatType switch
        {
            CombatType.Bukijutsu => (attacker.Character.Bukijutsu * attacker.Character.Strength) / 1000,
            CombatType.Ninjutsu => (attacker.Character.Ninjutsu * attacker.Character.Intelligence) / 1000,
            CombatType.Taijutsu => (attacker.Character.Taijutsu * attacker.Character.Speed) / 1000,
            CombatType.Genjutsu => (attacker.Character.Genjutsu * attacker.Character.Willpower) / 1000,
            _ => 0
        };
        
        // Base damage: Attack - Defense (minimum 1)
        var damage = Math.Max(1, attackStat - target.Character.DefenseValue);
        
        // Apply elemental modifiers if applicable
        if (request.Element.HasValue && target.Character.Element.HasValue)
        {
            var multiplier = GetElementalMultiplier(request.Element.Value, target.Character.Element.Value);
            damage = (int)(damage * multiplier);
        }
        
        return Math.Max(1, damage);
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
            _ => 1.0f
        };
    }
    
    private int CalculateHealing(BattleParticipant healer, CombatActionRequest request)
    {
        var baseHealing = healer.Character.MedNinRank switch
        {
            MedNinRank.NoviceMedic => 50,
            MedNinRank.FieldMedic => 100,
            MedNinRank.MasterMedic => 200,
            MedNinRank.LegendaryHealer => 400,
            _ => 50
        };
        
        baseHealing += healer.Character.Intelligence / 1000;
        return baseHealing;
    }
    
    private void ApplyDamage(int damage, int targetCharacterId, BattleGrid battle)
    {
        var target = battle.Participants.FirstOrDefault(p => p.CharacterId == targetCharacterId);
        if (target == null) return;
        
        target.CurrentHP = Math.Max(0, target.CurrentHP - damage);
        
        if (target.CurrentHP == 0)
        {
            target.IsDefeated = true;
            target.Character.IsInBattle = false;
            
            // Check if battle should end
            var activeParticipants = battle.Participants.Count(p => !p.IsDefeated);
            if (activeParticipants <= 1)
            {
                battle.EndBattle();
            }
        }
    }
    
    private void ApplyHealing(int healing, int targetCharacterId, BattleGrid battle)
    {
        var target = battle.Participants.FirstOrDefault(p => p.CharacterId == targetCharacterId);
        if (target == null) return;
        
        target.CurrentHP = Math.Min(target.Character.MaxHP, target.CurrentHP + healing);
    }
}

// Request/Response models
public class CreateBattleRequest
{
    public int CharacterId { get; set; }
    public BattleType BattleType { get; set; }
    public int? MissionId { get; set; }
}

public class JoinBattleRequest
{
    public int BattleId { get; set; }
    public int CharacterId { get; set; }
}

public class CombatActionRequest
{
    public int BattleId { get; set; }
    public int CharacterId { get; set; }
    public ActionType ActionType { get; set; }
    public int APCost { get; set; }
    public int CPCost { get; set; } = 0;
    public int SPCost { get; set; } = 0;
    public int? TargetCharacterId { get; set; }
    public int? TargetRow { get; set; }
    public int? TargetColumn { get; set; }
    public CombatType? CombatType { get; set; }
    public Element? Element { get; set; }
    public int? ItemId { get; set; }
}

public class CombatActionResult
{
    public bool IsSuccessful { get; set; }
    public int? DamageDealt { get; set; }
    public int? HealingDone { get; set; }
    public string? Message { get; set; }
}
