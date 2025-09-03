using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NinjaMMORPG.Core.Entities;
using NinjaMMORPG.Core.Enums;
using System.Security.Claims;

namespace NinjaMMORPG.API.Hubs;

[Authorize]
public class CombatHub : Hub
{
    private readonly ILogger<CombatHub> _logger;
    
    public CombatHub(ILogger<CombatHub> logger)
    {
        _logger = logger;
    }
    
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("User {UserId} connected to CombatHub", userId);
        
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("User {UserId} disconnected from CombatHub", userId);
        
        await base.OnDisconnectedAsync(exception);
    }
    
    // Join a battle
    public async Task JoinBattle(int battleId)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Battle_{battleId}");
            _logger.LogInformation("User joined battle {BattleId}", battleId);
            
            // Notify other participants
            await Clients.OthersInGroup($"Battle_{battleId}").SendAsync("PlayerJoinedBattle", Context.User?.Identity?.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining battle {BattleId}", battleId);
            throw;
        }
    }
    
    // Leave a battle
    public async Task LeaveBattle(int battleId)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Battle_{battleId}");
            _logger.LogInformation("User left battle {BattleId}", battleId);
            
            // Notify other participants
            await Clients.OthersInGroup($"Battle_{battleId}").SendAsync("PlayerLeftBattle", Context.User?.Identity?.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving battle {BattleId}", battleId);
            throw;
        }
    }
    
    // Send combat action to battle participants
    public async Task SendCombatAction(int battleId, object actionData)
    {
        try
        {
            _logger.LogInformation("Combat action received for battle {BattleId}", battleId);
            
            // Broadcast action to all battle participants
            await Clients.Group($"Battle_{battleId}").SendAsync("CombatActionReceived", actionData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending combat action for battle {BattleId}", battleId);
            throw;
        }
    }
    
    // Send movement update
    public async Task SendMovement(int battleId, int characterId, int newRow, int newColumn)
    {
        try
        {
            var movementData = new
            {
                CharacterId = characterId,
                NewRow = newRow,
                NewColumn = newColumn,
                Timestamp = DateTime.UtcNow
            };
            
            await Clients.Group($"Battle_{battleId}").SendAsync("MovementUpdate", movementData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending movement update for battle {BattleId}", battleId);
            throw;
        }
    }
    
    // Send chat message in battle
    public async Task SendBattleChat(int battleId, string message)
    {
        try
        {
            var chatData = new
            {
                Sender = Context.User?.Identity?.Name,
                Message = message,
                Timestamp = DateTime.UtcNow
            };
            
            await Clients.Group($"Battle_{battleId}").SendAsync("BattleChatMessage", chatData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending battle chat message for battle {BattleId}", battleId);
            throw;
        }
    }
    
    // Ready up for battle
    public async Task ReadyUp(int battleId)
    {
        try
        {
            var readyData = new
            {
                PlayerId = Context.User?.Identity?.Name,
                IsReady = true,
                Timestamp = DateTime.UtcNow
            };
            
            await Clients.Group($"Battle_{battleId}").SendAsync("PlayerReady", readyData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ready status for battle {BattleId}", battleId);
            throw;
        }
    }
    
    // Surrender from battle
    public async Task Surrender(int battleId)
    {
        try
        {
            var surrenderData = new
            {
                PlayerId = Context.User?.Identity?.Name,
                Timestamp = DateTime.UtcNow
            };
            
            await Clients.Group($"Battle_{battleId}").SendAsync("PlayerSurrendered", surrenderData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending surrender for battle {BattleId}", battleId);
            throw;
        }
    }
}
