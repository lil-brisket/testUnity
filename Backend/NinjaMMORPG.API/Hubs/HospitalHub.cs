using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace NinjaMMORPG.API.Hubs;

[Authorize]
public class HospitalHub : Hub
{
    private readonly ILogger<HospitalHub> _logger;
    
    public HospitalHub(ILogger<HospitalHub> logger)
    {
        _logger = logger;
    }
    
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.Identity?.Name;
        
        _logger.LogInformation("User {Username} ({UserId}) connected to HospitalHub", username, userId);
        
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.Identity?.Name;
        
        _logger.LogInformation("User {Username} ({UserId}) disconnected from HospitalHub", username, userId);
        
        await base.OnDisconnectedAsync(exception);
    }
    
    // Join hospital
    public async Task JoinHospital(string villageName)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Hospital_{villageName}");
            _logger.LogInformation("User {Username} joined hospital in {VillageName}", Context.User?.Identity?.Name, villageName);
            
            // Notify other hospital occupants
            await Clients.OthersInGroup($"Hospital_{villageName}").SendAsync("PlayerJoinedHospital", new
            {
                Username = Context.User?.Identity?.Name,
                Village = villageName,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining hospital in {VillageName}", villageName);
            throw;
        }
    }
    
    // Leave hospital
    public async Task LeaveHospital(string villageName)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Hospital_{villageName}");
            _logger.LogInformation("User {Username} left hospital in {VillageName}", Context.User?.Identity?.Name, villageName);
            
            // Notify other hospital occupants
            await Clients.OthersInGroup($"Hospital_{villageName}").SendAsync("PlayerLeftHospital", new
            {
                Username = Context.User?.Identity?.Name,
                Village = villageName,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving hospital in {VillageName}", villageName);
            throw;
        }
    }
    
    // Player enters hospital (0 HP)
    public async Task PlayerEnteredHospital(string villageName, string playerName, int currentHP)
    {
        try
        {
            var hospitalData = new
            {
                PlayerName = playerName,
                Village = villageName,
                CurrentHP = currentHP,
                Action = "Entered",
                Timestamp = DateTime.UtcNow
            };
            
            // Notify all hospital occupants
            await Clients.Group($"Hospital_{villageName}").SendAsync("PlayerEnteredHospital", hospitalData);
            
            // Notify village members
            await Clients.Group($"Village_{villageName}").SendAsync("PlayerEnteredHospital", hospitalData);
            
            _logger.LogInformation("Player {PlayerName} entered hospital in {VillageName} with {CurrentHP} HP", 
                playerName, villageName, currentHP);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying player entered hospital in {VillageName}", villageName);
            throw;
        }
    }
    
    // Medical ninja starts healing
    public async Task StartHealing(string villageName, string healerName, string targetName, int healingAmount)
    {
        try
        {
            var healingData = new
            {
                HealerName = healerName,
                TargetName = targetName,
                Village = villageName,
                HealingAmount = healingAmount,
                Action = "Started",
                Timestamp = DateTime.UtcNow
            };
            
            // Notify hospital occupants
            await Clients.Group($"Hospital_{villageName}").SendAsync("HealingStarted", healingData);
            
            _logger.LogInformation("Medical ninja {HealerName} started healing {TargetName} for {HealingAmount} HP in {VillageName}", 
                healerName, targetName, healingAmount, villageName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting healing in {VillageName}", villageName);
            throw;
        }
    }
    
    // Healing completed
    public async Task HealingCompleted(string villageName, string healerName, string targetName, int healingAmount, int newHP)
    {
        try
        {
            var healingData = new
            {
                HealerName = healerName,
                TargetName = targetName,
                Village = villageName,
                HealingAmount = healingAmount,
                NewHP = newHP,
                Action = "Completed",
                Timestamp = DateTime.UtcNow
            };
            
            // Notify hospital occupants
            await Clients.Group($"Hospital_{villageName}").SendAsync("HealingCompleted", healingData);
            
            // Notify village members
            await Clients.Group($"Village_{villageName}").SendAsync("HealingCompleted", healingData);
            
            _logger.LogInformation("Medical ninja {HealerName} completed healing {TargetName} for {HealingAmount} HP, new HP: {NewHP} in {VillageName}", 
                healerName, targetName, healingAmount, newHP, villageName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing healing in {VillageName}", villageName);
            throw;
        }
    }
    
    // Player recovered naturally
    public async Task NaturalRecovery(string villageName, string playerName, int recoveredHP)
    {
        try
        {
            var recoveryData = new
            {
                PlayerName = playerName,
                Village = villageName,
                RecoveredHP = recoveredHP,
                Action = "NaturalRecovery",
                Timestamp = DateTime.UtcNow
            };
            
            // Notify hospital occupants
            await Clients.Group($"Hospital_{villageName}").SendAsync("NaturalRecovery", recoveryData);
            
            // Notify village members
            await Clients.Group($"Village_{villageName}").SendAsync("NaturalRecovery", recoveryData);
            
            _logger.LogInformation("Player {PlayerName} recovered naturally for {RecoveredHP} HP in {VillageName}", 
                playerName, recoveredHP, villageName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying natural recovery in {VillageName}", villageName);
            throw;
        }
    }
    
    // Player paid for instant recovery
    public async Task PaidRecovery(string villageName, string playerName, int cost, int recoveredHP)
    {
        try
        {
            var recoveryData = new
            {
                PlayerName = playerName,
                Village = villageName,
                Cost = cost,
                RecoveredHP = recoveredHP,
                Action = "PaidRecovery",
                Timestamp = DateTime.UtcNow
            };
            
            // Notify hospital occupants
            await Clients.Group($"Hospital_{villageName}").SendAsync("PaidRecovery", recoveryData);
            
            _logger.LogInformation("Player {PlayerName} paid {Cost} Ryo for instant recovery of {RecoveredHP} HP in {VillageName}", 
                playerName, cost, recoveredHP, villageName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying paid recovery in {VillageName}", villageName);
            throw;
        }
    }
    
    // Medical ninja gained experience
    public async Task MedNinExperienceGained(string villageName, string healerName, int experienceGained, string newRank)
    {
        try
        {
            var experienceData = new
            {
                HealerName = healerName,
                Village = villageName,
                ExperienceGained = experienceGained,
                NewRank = newRank,
                Action = "ExperienceGained",
                Timestamp = DateTime.UtcNow
            };
            
            // Notify hospital occupants
            await Clients.Group($"Hospital_{villageName}").SendAsync("MedNinExperienceGained", experienceData);
            
            // Notify village members
            await Clients.Group($"Village_{villageName}").SendAsync("MedNinExperienceGained", experienceData);
            
            _logger.LogInformation("Medical ninja {HealerName} gained {ExperienceGained} experience, new rank: {NewRank} in {VillageName}", 
                healerName, experienceGained, newRank, villageName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying medical ninja experience gain in {VillageName}", villageName);
            throw;
        }
    }
    
    // Hospital status update
    public async Task HospitalStatusUpdate(string villageName, int injuredCount, int healerCount)
    {
        try
        {
            var statusData = new
            {
                Village = villageName,
                InjuredCount = injuredCount,
                HealerCount = healerCount,
                Timestamp = DateTime.UtcNow
            };
            
            // Notify hospital occupants
            await Clients.Group($"Hospital_{villageName}").SendAsync("HospitalStatusUpdate", statusData);
            
            _logger.LogInformation("Hospital status update in {VillageName}: {InjuredCount} injured, {HealerCount} healers", 
                villageName, injuredCount, healerCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hospital status in {VillageName}", villageName);
            throw;
        }
    }
}
