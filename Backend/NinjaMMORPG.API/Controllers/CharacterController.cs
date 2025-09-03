using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NinjaMMORPG.Core.Entities;
using NinjaMMORPG.Core.Enums;
using NinjaMMORPG.Infrastructure.Data;
using System.Security.Claims;

namespace NinjaMMORPG.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CharacterController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CharacterController> _logger;
    
    public CharacterController(ApplicationDbContext context, ILogger<CharacterController> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    // GET: api/Character
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Character>>> GetCharacters()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var characters = await _context.Characters
            .Where(c => c.UserId == userId)
            .Include(c => c.Clan)
            .Include(c => c.Equipment)
            .Include(c => c.BankAccount)
            .ToListAsync();
            
        return Ok(characters);
    }
    
    // GET: api/Character/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Character>> GetCharacter(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var character = await _context.Characters
            .Include(c => c.Clan)
            .Include(c => c.Equipment)
            .Include(c => c.BankAccount)
            .Include(c => c.Sensei)
            .Include(c => c.Students)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            
        if (character == null)
            return NotFound();
            
        return Ok(character);
    }
    
    // POST: api/Character
    [HttpPost]
    public async Task<ActionResult<Character>> CreateCharacter(CreateCharacterRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        // Check if user already has a character
        var existingCharacter = await _context.Characters
            .FirstOrDefaultAsync(c => c.UserId == userId);
            
        if (existingCharacter != null)
            return BadRequest("User already has a character");
            
        // Validate character name
        if (await _context.Characters.AnyAsync(c => c.Name == request.Name))
            return BadRequest("Character name already taken");
            
        // Validate village selection
        if (!Enum.IsDefined(typeof(Village), request.Village))
            return BadRequest("Invalid village selection");
            
        // Create character
        var character = new Character
        {
            Name = request.Name,
            Gender = request.Gender,
            Village = request.Village,
            UserId = userId,
            HP = 100,
            MaxHP = 100,
            CP = 100,
            MaxCP = 100,
            SP = 100,
            MaxSP = 100,
            Strength = 1,
            Intelligence = 1,
            Speed = 1,
            Willpower = 1,
            Ninjutsu = 1,
            Genjutsu = 1,
            Bukijutsu = 1,
            Taijutsu = 1,
            Level = 1,
            Rank = Rank.Student,
            MedNinRank = MedNinRank.NoviceMedic,
            PocketMoney = 1000,
            CurrentLocationId = 1, // Starting location
            LastActivity = DateTime.UtcNow
        };
        
        // Create bank account
        var bankAccount = new BankAccount
        {
            Character = character,
            Balance = 0,
            InterestRate = 0.02m,
            LastInterestCalculation = DateTime.UtcNow
        };
        
        _context.Characters.Add(character);
        _context.BankAccounts.Add(bankAccount);
        
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Character {CharacterName} created for user {UserId}", character.Name, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating character for user {UserId}", userId);
            return StatusCode(500, "Error creating character");
        }
        
        return CreatedAtAction(nameof(GetCharacter), new { id = character.Id }, character);
    }
    
    // PUT: api/Character/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCharacter(int id, UpdateCharacterRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var character = await _context.Characters
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            
        if (character == null)
            return NotFound();
            
        // Update allowed fields
        if (!string.IsNullOrEmpty(request.Name))
        {
            // Check if name is already taken by another character
            if (await _context.Characters.AnyAsync(c => c.Name == request.Name && c.Id != id))
                return BadRequest("Character name already taken");
                
            character.Name = request.Name;
        }
        
        if (request.Gender.HasValue)
            character.Gender = request.Gender.Value;
            
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Character {CharacterId} updated for user {UserId}", id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating character {CharacterId} for user {UserId}", id, userId);
            return StatusCode(500, "Error updating character");
        }
        
        return NoContent();
    }
    
    // POST: api/Character/5/train
    [HttpPost("{id}/train")]
    public async Task<ActionResult<TrainingResult>> TrainCharacter(int id, TrainingRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var character = await _context.Characters
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            
        if (character == null)
            return NotFound();
            
        // Validate training type
        if (!Enum.IsDefined(typeof(CombatType), request.TrainingType))
            return BadRequest("Invalid training type");
            
        // Calculate training cost and gains
        var trainingCost = CalculateTrainingCost(character, request.TrainingType);
        var trainingGain = CalculateTrainingGain(character, request.TrainingType);
        
        // Check if character has enough money
        if (character.PocketMoney < trainingCost)
            return BadRequest("Insufficient funds for training");
            
        // Apply training
        ApplyTraining(character, request.TrainingType, trainingGain);
        character.PocketMoney -= trainingCost;
        
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Character {CharacterId} trained {TrainingType} for {TrainingGain} points, cost: {TrainingCost}", 
                id, request.TrainingType, trainingGain, trainingCost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training character {CharacterId}", id);
            return StatusCode(500, "Error applying training");
        }
        
        var result = new TrainingResult
        {
            TrainingType = request.TrainingType,
            PointsGained = trainingGain,
            Cost = trainingCost,
            NewBalance = character.PocketMoney
        };
        
        return Ok(result);
    }
    
    // POST: api/Character/5/advance-rank
    [HttpPost("{id}/advance-rank")]
    public async Task<ActionResult<RankAdvancementResult>> AdvanceRank(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var character = await _context.Characters
            .Include(c => c.CompletedMissions)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            
        if (character == null)
            return NotFound();
            
        if (!character.CanAdvanceRank())
            return BadRequest("Character does not meet requirements for rank advancement");
            
        var oldRank = character.Rank;
        var newRank = GetNextRank(character.Rank);
        
        character.Rank = newRank;
        
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Character {CharacterId} advanced from {OldRank} to {NewRank}", id, oldRank, newRank);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error advancing rank for character {CharacterId}", id);
            return StatusCode(500, "Error advancing rank");
        }
        
        var result = new RankAdvancementResult
        {
            OldRank = oldRank,
            NewRank = newRank,
            AdvancementDate = DateTime.UtcNow
        };
        
        return Ok(result);
    }
    
    // Helper methods
    private int CalculateTrainingCost(Character character, CombatType trainingType)
    {
        // Base cost increases with character level and current stat value
        var baseCost = 100;
        var levelMultiplier = 1 + (character.Level - 1) * 0.1;
        
        var currentStat = trainingType switch
        {
            CombatType.Ninjutsu => character.Ninjutsu,
            CombatType.Genjutsu => character.Genjutsu,
            CombatType.Bukijutsu => character.Bukijutsu,
            CombatType.Taijutsu => character.Taijutsu,
            _ => 0
        };
        
        var statMultiplier = 1 + (currentStat / 10000.0);
        
        return (int)(baseCost * levelMultiplier * statMultiplier);
    }
    
    private int CalculateTrainingGain(Character character, CombatType trainingType)
    {
        // Base gain decreases as stats get higher
        var baseGain = 100;
        var currentStat = trainingType switch
        {
            CombatType.Ninjutsu => character.Ninjutsu,
            CombatType.Genjutsu => character.Genjutsu,
            CombatType.Bukijutsu => character.Bukijutsu,
            CombatType.Taijutsu => character.Taijutsu,
            _ => 0
        };
        
        // Diminishing returns
        var statMultiplier = Math.Max(0.1, 1.0 - (currentStat / 500000.0));
        
        return (int)(baseGain * statMultiplier);
    }
    
    private void ApplyTraining(Character character, CombatType trainingType, int gain)
    {
        switch (trainingType)
        {
            case CombatType.Ninjutsu:
                character.Ninjutsu = Math.Min(500000, character.Ninjutsu + gain);
                break;
            case CombatType.Genjutsu:
                character.Genjutsu = Math.Min(500000, character.Genjutsu + gain);
                break;
            case CombatType.Bukijutsu:
                character.Bukijutsu = Math.Min(500000, character.Bukijutsu + gain);
                break;
            case CombatType.Taijutsu:
                character.Taijutsu = Math.Min(500000, character.Taijutsu + gain);
                break;
        }
    }
    
    private Rank GetNextRank(Rank currentRank)
    {
        return currentRank switch
        {
            Rank.Student => Rank.Genin,
            Rank.Genin => Rank.Chunin,
            Rank.Chunin => Rank.Jounin,
            Rank.Jounin => Rank.SpecialJounin,
            _ => currentRank
        };
    }
}

// Request/Response models
public class CreateCharacterRequest
{
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public Village Village { get; set; }
}

public class UpdateCharacterRequest
{
    public string? Name { get; set; }
    public Gender? Gender { get; set; }
}

public class TrainingRequest
{
    public CombatType TrainingType { get; set; }
}

public class TrainingResult
{
    public CombatType TrainingType { get; set; }
    public int PointsGained { get; set; }
    public int Cost { get; set; }
    public int NewBalance { get; set; }
}

public class RankAdvancementResult
{
    public Rank OldRank { get; set; }
    public Rank NewRank { get; set; }
    public DateTime AdvancementDate { get; set; }
}
