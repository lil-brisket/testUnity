using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;

namespace NinjaMMORPG.Managers
{
    public class SocialManager : MonoBehaviour
    {
        public static SocialManager Instance { get; private set; }

        [Header("Social Settings")]
        [SerializeField] private int maxFriends = 100;
        [SerializeField] private int maxClanMembers = 50;
        [SerializeField] private float chatCooldown = 1f;
        [SerializeField] private int maxChatHistory = 100;

        // Social data
        private List<Friend> friendsList = new List<Friend>();
        private List<ChatMessage> globalChatHistory = new List<ChatMessage>();
        private List<ChatMessage> villageChatHistory = new List<ChatMessage>();
        private List<ChatMessage> clanChatHistory = new List<ChatMessage>();
        private Dictionary<string, List<ChatMessage>> privateChatHistory = new Dictionary<string, List<ChatMessage>>();

        // Clan data
        private Clan currentClan;
        private List<ClanApplication> pendingApplications = new List<ClanApplication>();

        // Chat state
        private Dictionary<string, DateTime> lastChatTimes = new Dictionary<string, DateTime>();
        private ChatChannel currentChatChannel = ChatChannel.Global;

        // Events
        public event Action<ChatMessage> OnGlobalMessageReceived;
        public event Action<ChatMessage> OnVillageMessageReceived;
        public event Action<ChatMessage> OnClanMessageReceived;
        public event Action<ChatMessage> OnPrivateMessageReceived;
        public event Action<Friend> OnFriendAdded;
        public event Action<Friend> OnFriendRemoved;
        public event Action<Friend> OnFriendStatusChanged;
        public event Action<Clan> OnClanJoined;
        public event Action OnClanLeft;
        public event Action<ClanApplication> OnClanApplicationReceived;
        public event Action<ClanApplication> OnClanApplicationStatusChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeChatHistory();
        }

        #region Initialization

        private void InitializeChatHistory()
        {
            // Initialize private chat history for current user
            var currentCharacter = CharacterManager.Instance.GetCurrentCharacter();
            if (currentCharacter != null)
            {
                privateChatHistory[currentCharacter.Id] = new List<ChatMessage>();
            }
        }

        #endregion

        #region Chat System

        public async Task<bool> SendMessage(ChatChannel channel, string message, string targetId = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                UIManager.Instance.ShowNotification("Cannot send empty message");
                return false;
            }

            // Check chat cooldown
            if (!CheckChatCooldown(channel))
            {
                return false;
            }

            var currentCharacter = CharacterManager.Instance.GetCurrentCharacter();
            if (currentCharacter == null)
            {
                UIManager.Instance.ShowNotification("No character loaded");
                return false;
            }

            // Validate message length
            if (message.Length > 500)
            {
                UIManager.Instance.ShowNotification("Message too long (max 500 characters)");
                return false;
            }

            try
            {
                var chatMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    SenderId = currentCharacter.Id,
                    SenderName = currentCharacter.Name,
                    Message = message,
                    Channel = channel,
                    TargetId = targetId,
                    Timestamp = DateTime.UtcNow
                };

                // Send message to backend
                var success = await NetworkManager.Instance.SendChatMessage(chatMessage);
                
                if (success)
                {
                    // Add to local history
                    AddMessageToHistory(chatMessage);
                    
                    // Update cooldown
                    UpdateChatCooldown(channel);
                    
                    // Trigger events
                    TriggerChatEvent(chatMessage);
                    
                    return true;
                }
                else
                {
                    UIManager.Instance.ShowNotification("Failed to send message");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending chat message: {ex.Message}");
                UIManager.Instance.ShowNotification("An error occurred while sending message");
                return false;
            }
        }

        private bool CheckChatCooldown(ChatChannel channel)
        {
            var channelKey = channel.ToString();
            if (lastChatTimes.TryGetValue(channelKey, out var lastTime))
            {
                var timeSinceLastMessage = DateTime.UtcNow - lastTime;
                if (timeSinceLastMessage.TotalSeconds < chatCooldown)
                {
                    var remainingTime = chatCooldown - (float)timeSinceLastMessage.TotalSeconds;
                    UIManager.Instance.ShowNotification($"Chat cooldown: {remainingTime:F1}s remaining");
                    return false;
                }
            }
            return true;
        }

        private void UpdateChatCooldown(ChatChannel channel)
        {
            var channelKey = channel.ToString();
            lastChatTimes[channelKey] = DateTime.UtcNow;
        }

        private void AddMessageToHistory(ChatMessage message)
        {
            switch (message.Channel)
            {
                case ChatChannel.Global:
                    globalChatHistory.Add(message);
                    if (globalChatHistory.Count > maxChatHistory)
                        globalChatHistory.RemoveAt(0);
                    break;
                case ChatChannel.Village:
                    villageChatHistory.Add(message);
                    if (villageChatHistory.Count > maxChatHistory)
                        villageChatHistory.RemoveAt(0);
                    break;
                case ChatChannel.Clan:
                    clanChatHistory.Add(message);
                    if (clanChatHistory.Count > maxChatHistory)
                        clanChatHistory.RemoveAt(0);
                    break;
                case ChatChannel.Private:
                    var targetId = message.TargetId ?? message.SenderId;
                    if (!privateChatHistory.ContainsKey(targetId))
                        privateChatHistory[targetId] = new List<ChatMessage>();
                    
                    privateChatHistory[targetId].Add(message);
                    if (privateChatHistory[targetId].Count > maxChatHistory)
                        privateChatHistory[targetId].RemoveAt(0);
                    break;
            }
        }

        private void TriggerChatEvent(ChatMessage message)
        {
            switch (message.Channel)
            {
                case ChatChannel.Global:
                    OnGlobalMessageReceived?.Invoke(message);
                    break;
                case ChatChannel.Village:
                    OnVillageMessageReceived?.Invoke(message);
                    break;
                case ChatChannel.Clan:
                    OnClanMessageReceived?.Invoke(message);
                    break;
                case ChatChannel.Private:
                    OnPrivateMessageReceived?.Invoke(message);
                    break;
            }
        }

        public void ReceiveMessage(ChatMessage message)
        {
            // Add message to history
            AddMessageToHistory(message);
            
            // Trigger appropriate event
            TriggerChatEvent(message);
            
            // Show notification for private messages
            if (message.Channel == ChatChannel.Private)
            {
                var currentCharacter = CharacterManager.Instance.GetCurrentCharacter();
                if (currentCharacter != null && message.SenderId != currentCharacter.Id)
                {
                    UIManager.Instance.ShowNotification($"Private message from {message.SenderName}");
                }
            }
        }

        public List<ChatMessage> GetChatHistory(ChatChannel channel, string targetId = null)
        {
            switch (channel)
            {
                case ChatChannel.Global:
                    return new List<ChatMessage>(globalChatHistory);
                case ChatChannel.Village:
                    return new List<ChatMessage>(villageChatHistory);
                case ChatChannel.Clan:
                    return new List<ChatMessage>(clanChatHistory);
                case ChatChannel.Private:
                    if (targetId != null && privateChatHistory.ContainsKey(targetId))
                        return new List<ChatMessage>(privateChatHistory[targetId]);
                    return new List<ChatMessage>();
                default:
                    return new List<ChatMessage>();
            }
        }

        public void ClearChatHistory(ChatChannel channel)
        {
            switch (channel)
            {
                case ChatChannel.Global:
                    globalChatHistory.Clear();
                    break;
                case ChatChannel.Village:
                    villageChatHistory.Clear();
                    break;
                case ChatChannel.Clan:
                    clanChatHistory.Clear();
                    break;
                case ChatChannel.Private:
                    privateChatHistory.Clear();
                    break;
            }
        }

        #endregion

        #region Friends System

        public async Task<bool> AddFriend(string friendName)
        {
            if (friendsList.Count >= maxFriends)
            {
                UIManager.Instance.ShowNotification("Friends list is full");
                return false;
            }

            if (friendsList.Any(f => f.Name == friendName))
            {
                UIManager.Instance.ShowNotification("Already friends with this person");
                return false;
            }

            try
            {
                var success = await NetworkManager.Instance.AddFriend(friendName);
                
                if (success)
                {
                    var friend = new Friend
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = friendName,
                        Status = FriendStatus.Pending,
                        AddedAt = DateTime.UtcNow
                    };
                    
                    friendsList.Add(friend);
                    OnFriendAdded?.Invoke(friend);
                    
                    UIManager.Instance.ShowNotification($"Friend request sent to {friendName}");
                    return true;
                }
                else
                {
                    UIManager.Instance.ShowNotification("Failed to add friend");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error adding friend: {ex.Message}");
                UIManager.Instance.ShowNotification("An error occurred while adding friend");
                return false;
            }
        }

        public async Task<bool> RemoveFriend(string friendId)
        {
            var friend = friendsList.FirstOrDefault(f => f.Id == friendId);
            if (friend == null)
            {
                UIManager.Instance.ShowNotification("Friend not found");
                return false;
            }

            try
            {
                var success = await NetworkManager.Instance.RemoveFriend(friendId);
                
                if (success)
                {
                    friendsList.Remove(friend);
                    OnFriendRemoved?.Invoke(friend);
                    
                    UIManager.Instance.ShowNotification($"Removed {friend.Name} from friends list");
                    return true;
                }
                else
                {
                    UIManager.Instance.ShowNotification("Failed to remove friend");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error removing friend: {ex.Message}");
                UIManager.Instance.ShowNotification("An error occurred while removing friend");
                return false;
            }
        }

        public void UpdateFriendStatus(string friendId, FriendStatus status)
        {
            var friend = friendsList.FirstOrDefault(f => f.Id == friendId);
            if (friend != null && friend.Status != status)
            {
                friend.Status = status;
                OnFriendStatusChanged?.Invoke(friend);
            }
        }

        public List<Friend> GetFriendsList()
        {
            return new List<Friend>(friendsList);
        }

        public List<Friend> GetOnlineFriends()
        {
            return friendsList.Where(f => f.Status == FriendStatus.Online).ToList();
        }

        public Friend GetFriend(string friendId)
        {
            return friendsList.FirstOrDefault(f => f.Id == friendId);
        }

        #endregion

        #region Clan System

        public async Task<bool> CreateClan(string clanName, string description)
        {
            if (currentClan != null)
            {
                UIManager.Instance.ShowNotification("Already in a clan");
                return false;
            }

            if (string.IsNullOrEmpty(clanName) || clanName.Length < 3 || clanName.Length > 20)
            {
                UIManager.Instance.ShowNotification("Clan name must be between 3 and 20 characters");
                return false;
            }

            try
            {
                var clanData = new ClanData
                {
                    Name = clanName,
                    Description = description,
                    ShogunId = CharacterManager.Instance.GetCurrentCharacter()?.Id,
                    CreatedAt = DateTime.UtcNow
                };

                var success = await NetworkManager.Instance.CreateClan(clanData);
                
                if (success)
                {
                    // Load the created clan
                    await LoadClanData(clanName);
                    UIManager.Instance.ShowNotification($"Clan '{clanName}' created successfully!");
                    return true;
                }
                else
                {
                    UIManager.Instance.ShowNotification("Failed to create clan");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating clan: {ex.Message}");
                UIManager.Instance.ShowNotification("An error occurred while creating clan");
                return false;
            }
        }

        public async Task<bool> JoinClan(string clanName)
        {
            if (currentClan != null)
            {
                UIManager.Instance.ShowNotification("Already in a clan");
                return false;
            }

            try
            {
                var success = await NetworkManager.Instance.JoinClan(clanName);
                
                if (success)
                {
                    // Load clan data
                    await LoadClanData(clanName);
                    UIManager.Instance.ShowNotification($"Joined clan '{clanName}'!");
                    return true;
                }
                else
                {
                    UIManager.Instance.ShowNotification("Failed to join clan");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error joining clan: {ex.Message}");
                UIManager.Instance.ShowNotification("An error occurred while joining clan");
                return false;
            }
        }

        public async Task<bool> LeaveClan()
        {
            if (currentClan == null)
            {
                UIManager.Instance.ShowNotification("Not in a clan");
                return false;
            }

            try
            {
                var success = await NetworkManager.Instance.LeaveClan();
                
                if (success)
                {
                    var clanName = currentClan.Name;
                    currentClan = null;
                    
                    OnClanLeft?.Invoke();
                    UIManager.Instance.ShowNotification($"Left clan '{clanName}'");
                    return true;
                }
                else
                {
                    UIManager.Instance.ShowNotification("Failed to leave clan");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error leaving clan: {ex.Message}");
                UIManager.Instance.ShowNotification("An error occurred while leaving clan");
                return false;
            }
        }

        private async Task<bool> LoadClanData(string clanName)
        {
            try
            {
                var clanData = await NetworkManager.Instance.GetClan(clanName);
                
                if (clanData != null)
                {
                    currentClan = new Clan(clanData);
                    OnClanJoined?.Invoke(currentClan);
                    return true;
                }
                else
                {
                    UIManager.Instance.ShowNotification("Failed to load clan data");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading clan data: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ProcessClanApplication(string applicantId, bool approved)
        {
            if (currentClan == null)
            {
                UIManager.Instance.ShowNotification("Not in a clan");
                return false;
            }

            var currentCharacter = CharacterManager.Instance.GetCurrentCharacter();
            if (currentCharacter?.Id != currentClan.ShogunId)
            {
                UIManager.Instance.ShowNotification("Only the clan leader can process applications");
                return false;
            }

            try
            {
                var success = await NetworkManager.Instance.ProcessClanApplication(applicantId, approved);
                
                if (success)
                {
                    // Remove from pending applications
                    var application = pendingApplications.FirstOrDefault(a => a.ApplicantId == applicantId);
                    if (application != null)
                    {
                        application.Status = approved ? ApplicationStatus.Approved : ApplicationStatus.Rejected;
                        pendingApplications.Remove(application);
                        OnClanApplicationStatusChanged?.Invoke(application);
                    }

                    var action = approved ? "approved" : "rejected";
                    UIManager.Instance.ShowNotification($"Clan application {action}");
                    return true;
                }
                else
                {
                    UIManager.Instance.ShowNotification("Failed to process application");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing clan application: {ex.Message}");
                UIManager.Instance.ShowNotification("An error occurred while processing application");
                return false;
            }
        }

        public void AddClanApplication(ClanApplication application)
        {
            pendingApplications.Add(application);
            OnClanApplicationReceived?.Invoke(application);
        }

        public List<ClanApplication> GetPendingApplications()
        {
            return new List<ClanApplication>(pendingApplications);
        }

        public Clan GetCurrentClan()
        {
            return currentClan;
        }

        #endregion

        #region Utility Methods

        public void SetCurrentChatChannel(ChatChannel channel)
        {
            currentChatChannel = channel;
        }

        public ChatChannel GetCurrentChatChannel()
        {
            return currentChatChannel;
        }

        public float GetChatCooldownRemaining(ChatChannel channel)
        {
            var channelKey = channel.ToString();
            if (!lastChatTimes.TryGetValue(channelKey, out var lastTime))
                return 0f;

            var timeSinceLastMessage = DateTime.UtcNow - lastTime;
            var remaining = chatCooldown - (float)timeSinceLastMessage.TotalSeconds;
            
            return Mathf.Max(0f, remaining);
        }

        public void ResetChatCooldowns()
        {
            lastChatTimes.Clear();
        }

        #endregion

        #region Public Interface

        public List<Friend> FriendsList => new List<Friend>(friendsList);
        public Clan CurrentClan => currentClan;
        public List<ClanApplication> PendingApplications => new List<ClanApplication>(pendingApplications);
        public int MaxFriends => maxFriends;
        public int MaxClanMembers => maxClanMembers;
        public float ChatCooldown => chatCooldown;
        public int MaxChatHistory => maxChatHistory;

        public async Task<bool> RefreshSocialData()
        {
            try
            {
                // Refresh friends list
                var friendsData = await NetworkManager.Instance.GetFriendsList();
                if (friendsData != null)
                {
                    friendsList.Clear();
                    foreach (var friendData in friendsData)
                    {
                        friendsList.Add(new Friend(friendData));
                    }
                }

                // Refresh clan data if in a clan
                if (currentClan != null)
                {
                    await LoadClanData(currentClan.Name);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error refreshing social data: {ex.Message}");
                return false;
            }
        }

        public void ClearAllData()
        {
            friendsList.Clear();
            currentClan = null;
            pendingApplications.Clear();
            globalChatHistory.Clear();
            villageChatHistory.Clear();
            clanChatHistory.Clear();
            privateChatHistory.Clear();
            lastChatTimes.Clear();
        }

        #endregion
    }

    #region Supporting Classes and Enums

    [System.Serializable]
    public class ChatMessage
    {
        public string Id;
        public string SenderId;
        public string SenderName;
        public string Message;
        public ChatChannel Channel;
        public string TargetId;
        public DateTime Timestamp;
    }

    [System.Serializable]
    public class Friend
    {
        public string Id;
        public string Name;
        public FriendStatus Status;
        public DateTime AddedAt;
        public DateTime LastSeen;
    }

    [System.Serializable]
    public class Clan
    {
        public string Id;
        public string Name;
        public string Description;
        public string ShogunId;
        public List<string> Advisors;
        public List<string> Members;
        public int MaxMembers;
        public float RyoInterestRate;
        public float TrainingBonus;
        public DateTime CreatedAt;
    }

    [System.Serializable]
    public class ClanApplication
    {
        public string Id;
        public string ClanId;
        public string ClanName;
        public string ApplicantId;
        public string ApplicantName;
        public string Message;
        public ApplicationStatus Status;
        public DateTime AppliedAt;
    }

    public enum ChatChannel
    {
        Global,
        Village,
        Clan,
        Private
    }

    public enum FriendStatus
    {
        Offline,
        Online,
        Away,
        Busy,
        Pending
    }

    public enum ApplicationStatus
    {
        Pending,
        Approved,
        Rejected
    }

    #endregion
}
