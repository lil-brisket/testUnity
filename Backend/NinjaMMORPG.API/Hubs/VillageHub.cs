using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace NinjaMMORPG.API.Hubs;

[Authorize]
public class VillageHub : Hub
{
    private readonly ILogger<VillageHub> _logger;
    
    public VillageHub(ILogger<VillageHub> logger)
    {
        _logger = logger;
    }
    
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.Identity?.Name;
        
        _logger.LogInformation("User {Username} ({UserId}) connected to VillageHub", username, userId);
        
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.Identity?.Name;
        
        _logger.LogInformation("User {Username} ({UserId}) disconnected from VillageHub", username, userId);
        
        await base.OnDisconnectedAsync(exception);
    }
    
    // Join village
    public async Task JoinVillage(string villageName)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Village_{villageName}");
            _logger.LogInformation("User {Username} joined village: {VillageName}", Context.User?.Identity?.Name, villageName);
            
            // Notify other village members
            await Clients.OthersInGroup($"Village_{villageName}").SendAsync("PlayerJoinedVillage", new
            {
                Username = Context.User?.Identity?.Name,
                Village = villageName,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining village: {VillageName}", villageName);
            throw;
        }
    }
    
    // Leave village
    public async Task LeaveVillage(string villageName)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Village_{villageName}");
            _logger.LogInformation("User {Username} left village: {VillageName}", Context.User?.Identity?.Name, villageName);
            
            // Notify other village members
            await Clients.OthersInGroup($"Village_{villageName}").SendAsync("PlayerLeftVillage", new
            {
                Username = Context.User?.Identity?.Name,
                Village = villageName,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving village: {VillageName}", villageName);
            throw;
        }
    }
    
    // Enter building
    public async Task EnterBuilding(string villageName, string buildingName)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Building_{villageName}_{buildingName}");
            
            var buildingData = new
            {
                Username = Context.User?.Identity?.Name,
                Village = villageName,
                Building = buildingName,
                Action = "Entered",
                Timestamp = DateTime.UtcNow
            };
            
            // Notify building occupants
            await Clients.Group($"Building_{villageName}_{buildingName}").SendAsync("PlayerEnteredBuilding", buildingData);
            
            // Notify village members
            await Clients.Group($"Village_{villageName}").SendAsync("PlayerEnteredBuilding", buildingData);
            
            _logger.LogInformation("User {Username} entered {Building} in {Village}", Context.User?.Identity?.Name, buildingName, villageName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error entering building: {Building} in {Village}", buildingName, villageName);
            throw;
        }
    }
    
    // Leave building
    public async Task LeaveBuilding(string villageName, string buildingName)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Building_{villageName}_{buildingName}");
            
            var buildingData = new
            {
                Username = Context.User?.Identity?.Name,
                Village = villageName,
                Building = buildingName,
                Action = "Left",
                Timestamp = DateTime.UtcNow
            };
            
            // Notify building occupants
            await Clients.Group($"Building_{villageName}_{buildingName}").SendAsync("PlayerLeftBuilding", buildingData);
            
            // Notify village members
            await Clients.Group($"Village_{villageName}").SendAsync("PlayerLeftBuilding", buildingData);
            
            _logger.LogInformation("User {Username} left {Building} in {Village}", Context.User?.Identity?.Name, buildingName, villageName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving building: {Building} in {Village}", buildingName, villageName);
            throw;
        }
    }
    
    // Start training
    public async Task StartTraining(string villageName, string trainingType)
    {
        try
        {
            var trainingData = new
            {
                Username = Context.User?.Identity?.Name,
                Village = villageName,
                TrainingType = trainingType,
                Action = "Started",
                Timestamp = DateTime.UtcNow
            };
            
            // Notify village members
            await Clients.Group($"Village_{villageName}").SendAsync("TrainingStarted", trainingData);
            
            _logger.LogInformation("User {Username} started {TrainingType} training in {Village}", Context.User?.Identity?.Name, trainingType, villageName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting training: {TrainingType} in {Village}", trainingType, villageName);
            throw;
        }
    }
    
    // Complete training
    public async Task CompleteTraining(string villageName, string trainingType, int statGained)
    {
        try
        {
            var trainingData = new
            {
                Username = Context.User?.Identity?.Name,
                Village = villageName,
                TrainingType = trainingType,
                StatGained = statGained,
                Action = "Completed",
                Timestamp = DateTime.UtcNow
            };
            
            // Notify village members
            await Clients.Group($"Village_{villageName}").SendAsync("TrainingCompleted", trainingData);
            
            _logger.LogInformation("User {Username} completed {TrainingType} training in {Village}, gained {StatGained} points", 
                Context.User?.Identity?.Name, trainingType, villageName, statGained);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing training: {TrainingType} in {Village}", trainingType, villageName);
            throw;
        }
    }
    
    // Mission accepted
    public async Task MissionAccepted(string villageName, string missionTitle, string missionRank)
    {
        try
        {
            var missionData = new
            {
                Username = Context.User?.Identity?.Name,
                Village = villageName,
                MissionTitle = missionTitle,
                MissionRank = missionRank,
                Action = "Accepted",
                Timestamp = DateTime.UtcNow
            };
            
            // Notify village members
            await Clients.Group($"Village_{villageName}").SendAsync("MissionAccepted", missionData);
            
            _logger.LogInformation("User {Username} accepted {MissionRank} mission '{MissionTitle}' in {Village}", 
                Context.User?.Identity?.Name, missionRank, missionTitle, villageName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting mission in {Village}", villageName);
            throw;
        }
    }
    
    // Mission completed
    public async Task MissionCompleted(string villageName, string missionTitle, string missionRank, int reward)
    {
        try
        {
            var missionData = new
            {
                Username = Context.User?.Identity?.Name,
                Village = villageName,
                MissionTitle = missionTitle,
                MissionRank = missionRank,
                Reward = reward,
                Action = "Completed",
                Timestamp = DateTime.UtcNow
            };
            
            // Notify village members
            await Clients.Group($"Village_{villageName}").SendAsync("MissionCompleted", missionData);
            
            _logger.LogInformation("User {Username} completed {MissionRank} mission '{MissionTitle}' in {Village}, reward: {Reward}", 
                Context.User?.Identity?.Name, missionRank, missionTitle, villageName, reward);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing mission in {Village}", villageName);
            throw;
        }
    }
}
