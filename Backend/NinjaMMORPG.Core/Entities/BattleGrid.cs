using System.ComponentModel.DataAnnotations;
using NinjaMMORPG.Core.Enums;

namespace NinjaMMORPG.Core.Entities;

public class BattleGrid
{
    public int Id { get; set; }
    public int Rows { get; set; } = 5;
    public int Columns { get; set; } = 8;
    
    // Battle state
    public BattleStatus Status { get; set; } = BattleStatus.Preparing;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    // Battle type
    public BattleType Type { get; set; }
    public int? MissionId { get; set; }
    
    // Navigation Properties
    public virtual List<BattleParticipant> Participants { get; set; } = new();
    public virtual List<CombatAction> Actions { get; set; } = new();
    public virtual Mission? Mission { get; set; }
    
    // Computed Properties
    public bool IsActive => Status == BattleStatus.InProgress;
    public int ParticipantCount => Participants.Count;
    
    public void StartBattle()
    {
        if (Status == BattleStatus.Preparing && Participants.Count >= 1)
        {
            Status = BattleStatus.InProgress;
            StartDate = DateTime.UtcNow;
            
            // Initialize all participants with 100 AP
            foreach (var participant in Participants)
            {
                participant.CurrentAP = 100;
                participant.IsReady = false;
            }
        }
    }
    
    public void EndBattle()
    {
        Status = BattleStatus.Finished;
        EndDate = DateTime.UtcNow;
    }
    
    public bool IsValidPosition(int row, int column)
    {
        return row >= 0 && row < Rows && column >= 0 && column < Columns;
    }
    
    public bool IsPositionOccupied(int row, int column)
    {
        return Participants.Any(p => p.CurrentRow == row && p.CurrentColumn == column);
    }
    
    public int CalculateMovementCost(int fromRow, int fromCol, int toRow, int toCol)
    {
        // Manhattan distance for AP cost
        return Math.Abs(toRow - fromRow) + Math.Abs(toCol - fromCol);
    }
}

public class BattleParticipant
{
    public int Id { get; set; }
    public int BattleGridId { get; set; }
    public int CharacterId { get; set; }
    
    // Grid position
    public int CurrentRow { get; set; }
    public int CurrentColumn { get; set; }
    
    // Combat state
    public int CurrentAP { get; set; } = 100;
    public int CurrentHP { get; set; }
    public bool IsReady { get; set; } = false;
    public bool IsDefeated { get; set; } = false;
    
    // Turn order
    public int Initiative { get; set; }
    public DateTime? LastActionTime { get; set; }
    
    // Navigation Properties
    public virtual BattleGrid BattleGrid { get; set; } = null!;
    public virtual Character Character { get; set; } = null!;
    
    public bool CanMoveTo(int targetRow, int targetColumn)
    {
        if (!BattleGrid.IsValidPosition(targetRow, targetColumn))
            return false;
            
        if (BattleGrid.IsPositionOccupied(targetRow, targetColumn))
            return false;
            
        int movementCost = BattleGrid.CalculateMovementCost(CurrentRow, CurrentColumn, targetRow, targetColumn);
        return CurrentAP >= movementCost;
    }
    
    public bool MoveTo(int targetRow, int targetColumn)
    {
        if (!CanMoveTo(targetRow, targetColumn))
            return false;
            
        int movementCost = BattleGrid.CalculateMovementCost(CurrentRow, CurrentColumn, targetRow, targetColumn);
        CurrentAP -= movementCost;
        
        CurrentRow = targetRow;
        CurrentColumn = targetColumn;
        LastActionTime = DateTime.UtcNow;
        
        return true;
    }
    
    public bool CanPerformAction(int apCost)
    {
        return CurrentAP >= apCost && !IsDefeated;
    }
    
    public void ConsumeAP(int apCost)
    {
        CurrentAP = Math.Max(0, CurrentAP - apCost);
        LastActionTime = DateTime.UtcNow;
    }
}

public enum BattleStatus
{
    Preparing = 1,
    InProgress = 2,
    Paused = 3,
    Finished = 4
}

public enum BattleType
{
    PvE = 1,            // Player vs Environment
    PvP = 2,            // Player vs Player
    Mission = 3,        // Mission-related battle
    Training = 4        // Training spar
}
