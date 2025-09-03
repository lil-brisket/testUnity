using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace NinjaMMORPG.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    
    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }
    
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.Identity?.Name;
        
        _logger.LogInformation("User {Username} ({UserId}) connected to ChatHub", username, userId);
        
        // Add user to global chat
        await Groups.AddToGroupAsync(Context.ConnectionId, "Global");
        
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.Identity?.Name;
        
        _logger.LogInformation("User {Username} ({UserId}) disconnected from ChatHub", username, userId);
        
        await base.OnDisconnectedAsync(exception);
    }
    
    // Join village chat
    public async Task JoinVillageChat(string villageName)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Village_{villageName}");
            _logger.LogInformation("User joined village chat: {VillageName}", villageName);
            
            // Notify village members
            await Clients.Group($"Village_{villageName}").SendAsync("UserJoinedVillage", Context.User?.Identity?.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining village chat: {VillageName}", villageName);
            throw;
        }
    }
    
    // Leave village chat
    public async Task LeaveVillageChat(string villageName)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Village_{villageName}");
            _logger.LogInformation("User left village chat: {VillageName}", villageName);
            
            // Notify village members
            await Clients.Group($"Village_{villageName}").SendAsync("UserLeftVillage", Context.User?.Identity?.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving village chat: {VillageName}", villageName);
            throw;
        }
    }
    
    // Join clan chat
    public async Task JoinClanChat(int clanId)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Clan_{clanId}");
            _logger.LogInformation("User joined clan chat: {ClanId}", clanId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining clan chat: {ClanId}", clanId);
            throw;
        }
    }
    
    // Leave clan chat
    public async Task LeaveClanChat(int clanId)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Clan_{clanId}");
            _logger.LogInformation("User left clan chat: {ClanId}", clanId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving clan chat: {ClanId}", clanId);
            throw;
        }
    }
    
    // Send global chat message
    public async Task SendGlobalMessage(string message)
    {
        try
        {
            var chatData = new
            {
                Sender = Context.User?.Identity?.Name,
                Message = message,
                Timestamp = DateTime.UtcNow,
                ChatType = "Global"
            };
            
            await Clients.Group("Global").SendAsync("GlobalChatMessage", chatData);
            _logger.LogInformation("Global chat message sent by {Sender}", Context.User?.Identity?.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending global chat message");
            throw;
        }
    }
    
    // Send village chat message
    public async Task SendVillageMessage(string villageName, string message)
    {
        try
        {
            var chatData = new
            {
                Sender = Context.User?.Identity?.Name,
                Message = message,
                Timestamp = DateTime.UtcNow,
                ChatType = "Village",
                Village = villageName
            };
            
            await Clients.Group($"Village_{villageName}").SendAsync("VillageChatMessage", chatData);
            _logger.LogInformation("Village chat message sent by {Sender} in {Village}", Context.User?.Identity?.Name, villageName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending village chat message");
            throw;
        }
    }
    
    // Send clan chat message
    public async Task SendClanMessage(int clanId, string message)
    {
        try
        {
            var chatData = new
            {
                Sender = Context.User?.Identity?.Name,
                Message = message,
                Timestamp = DateTime.UtcNow,
                ChatType = "Clan",
                ClanId = clanId
            };
            
            await Clients.Group($"Clan_{clanId}").SendAsync("ClanChatMessage", chatData);
            _logger.LogInformation("Clan chat message sent by {Sender} in clan {ClanId}", Context.User?.Identity?.Name, clanId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending clan chat message");
            throw;
        }
    }
    
    // Send private message
    public async Task SendPrivateMessage(string recipientUsername, string message)
    {
        try
        {
            var chatData = new
            {
                Sender = Context.User?.Identity?.Name,
                Recipient = recipientUsername,
                Message = message,
                Timestamp = DateTime.UtcNow,
                ChatType = "Private"
            };
            
            // Send to recipient (they need to be connected)
            await Clients.User(recipientUsername).SendAsync("PrivateMessageReceived", chatData);
            
            // Send confirmation to sender
            await Clients.Caller.SendAsync("PrivateMessageSent", chatData);
            
            _logger.LogInformation("Private message sent from {Sender} to {Recipient}", Context.User?.Identity?.Name, recipientUsername);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending private message");
            throw;
        }
    }
    
    // Send emote
    public async Task SendEmote(string emoteType, string? targetUsername = null)
    {
        try
        {
            var emoteData = new
            {
                Sender = Context.User?.Identity?.Name,
                EmoteType = emoteType,
                Target = targetUsername,
                Timestamp = DateTime.UtcNow
            };
            
            if (string.IsNullOrEmpty(targetUsername))
            {
                // Global emote
                await Clients.Group("Global").SendAsync("EmoteReceived", emoteData);
            }
            else
            {
                // Targeted emote
                await Clients.User(targetUsername).SendAsync("EmoteReceived", emoteData);
                await Clients.Caller.SendAsync("EmoteSent", emoteData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending emote");
            throw;
        }
    }
}
