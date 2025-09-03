using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using NinjaMMORPG.Models;
using NinjaMMORPG.Enums;

namespace NinjaMMORPG.Managers
{
    public class NetworkManager : MonoBehaviour
    {
        [Header("Network Settings")]
        public string serverUrl = "https://localhost:5001";
        public string apiBaseUrl = "https://localhost:5001/api";
        
        [Header("Connection Status")]
        public bool isConnected = false;
        public bool isConnecting = false;
        public string connectionStatus = "Disconnected";
        
        // Events
        public event Action<bool> OnConnectionStatusChanged;
        public event Action<string> OnErrorReceived;
        public event Action<Character> OnCharacterReceived;
        public event Action<List<Mission>> OnMissionsReceived;
        
        // Private fields
        private string authToken;
        private Dictionary<string, object> signalRConnections = new Dictionary<string, object>();
        
        private void Start()
        {
            // Load saved auth token if available
            authToken = PlayerPrefs.GetString("AuthToken", "");
            
            if (!string.IsNullOrEmpty(authToken))
            {
                // Try to reconnect with saved token
                _ = ConnectToServer();
            }
        }
        
        public async Task<bool> ConnectToServer()
        {
            if (isConnecting) return false;
            
            isConnecting = true;
            connectionStatus = "Connecting...";
            
            try
            {
                // For now, we'll simulate a connection
                // In a real implementation, you'd establish SignalR connections here
                await Task.Delay(1000); // Simulate connection delay
                
                isConnected = true;
                isConnecting = false;
                connectionStatus = "Connected";
                
                OnConnectionStatusChanged?.Invoke(true);
                
                Debug.Log("Successfully connected to server");
                return true;
            }
            catch (Exception ex)
            {
                isConnected = false;
                isConnecting = false;
                connectionStatus = "Connection Failed";
                
                OnConnectionStatusChanged?.Invoke(false);
                OnErrorReceived?.Invoke($"Connection failed: {ex.Message}");
                
                Debug.LogError($"Failed to connect to server: {ex.Message}");
                return false;
            }
        }
        
        public void DisconnectFromServer()
        {
            isConnected = false;
            connectionStatus = "Disconnected";
            
            // Close all SignalR connections
            foreach (var connection in signalRConnections.Values)
            {
                // Dispose connection if it implements IDisposable
                if (connection is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            
            signalRConnections.Clear();
            
            OnConnectionStatusChanged?.Invoke(false);
            Debug.Log("Disconnected from server");
        }
        
        public void SetAuthToken(string token)
        {
            authToken = token;
            PlayerPrefs.SetString("AuthToken", token);
            PlayerPrefs.Save();
        }
        
        public void ClearAuthToken()
        {
            authToken = "";
            PlayerPrefs.DeleteKey("AuthToken");
            PlayerPrefs.Save();
        }
        
        // API Methods
        public async Task<Character> CreateCharacter(CreateCharacterRequest request)
        {
            if (!isConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }
            
            try
            {
                // Simulate API call
                await Task.Delay(500);
                
                var character = new Character
                {
                    Id = UnityEngine.Random.Range(1000, 9999),
                    Name = request.Name,
                    Gender = request.Gender,
                    Village = request.Village,
                    HP = 100,
                    MaxHP = 100,
                    CP = 100,
                    MaxCP = 100,
                    SP = 100,
                    MaxSP = 100
                };
                
                OnCharacterReceived?.Invoke(character);
                return character;
            }
            catch (Exception ex)
            {
                OnErrorReceived?.Invoke($"Failed to create character: {ex.Message}");
                throw;
            }
        }
        
        public async Task<Character> GetCharacter(int characterId)
        {
            if (!isConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }
            
            try
            {
                // Simulate API call
                await Task.Delay(300);
                
                // Return mock character for now
                var character = new Character
                {
                    Id = characterId,
                    Name = "Test Character",
                    Gender = Gender.Male,
                    Village = Village.HiddenLeaf,
                    HP = 100,
                    MaxHP = 100,
                    CP = 100,
                    MaxCP = 100,
                    SP = 100,
                    MaxSP = 100
                };
                
                OnCharacterReceived?.Invoke(character);
                return character;
            }
            catch (Exception ex)
            {
                OnErrorReceived?.Invoke($"Failed to get character: {ex.Message}");
                throw;
            }
        }
        
        public async Task<bool> UpdateCharacter(int characterId, UpdateCharacterRequest request)
        {
            if (!isConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }
            
            try
            {
                // Simulate API call
                await Task.Delay(300);
                
                Debug.Log($"Character {characterId} updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                OnErrorReceived?.Invoke($"Failed to update character: {ex.Message}");
                throw;
            }
        }
        
        public async Task<TrainingResult> TrainCharacter(int characterId, TrainingRequest request)
        {
            if (!isConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }
            
            try
            {
                // Simulate API call
                await Task.Delay(500);
                
                var result = new TrainingResult
                {
                    TrainingType = request.TrainingType,
                    PointsGained = UnityEngine.Random.Range(50, 150),
                    Cost = UnityEngine.Random.Range(100, 500),
                    NewBalance = UnityEngine.Random.Range(1000, 5000)
                };
                
                return result;
            }
            catch (Exception ex)
            {
                OnErrorReceived?.Invoke($"Failed to train character: {ex.Message}");
                throw;
            }
        }
        
        public async Task<bool> AdvanceRank(int characterId)
        {
            if (!isConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }
            
            try
            {
                // Simulate API call
                await Task.Delay(500);
                
                Debug.Log($"Character {characterId} rank advanced successfully");
                return true;
            }
            catch (Exception ex)
            {
                OnErrorReceived?.Invoke($"Failed to advance rank: {ex.Message}");
                throw;
            }
        }
        
        public async Task<List<Mission>> GetAvailableMissions(Rank playerRank)
        {
            if (!isConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }
            
            try
            {
                // Simulate API call
                await Task.Delay(400);
                
                var missions = new List<Mission>();
                
                // Generate mock missions based on player rank
                for (int i = 0; i < 5; i++)
                {
                    var missionRank = (MissionRank)UnityEngine.Random.Range(1, (int)playerRank + 1);
                    var missionType = (MissionType)UnityEngine.Random.Range(1, 7);
                    
                    missions.Add(new Mission
                    {
                        Id = i + 1,
                        Title = $"Mission {missionRank} - {missionType}",
                        Description = $"A {missionRank} rank {missionType} mission",
                        Rank = missionRank,
                        Type = missionType,
                        Village = Village.HiddenLeaf,
                        MinLevel = (int)missionRank * 2,
                        MinRank = (Rank)missionRank,
                        ExperienceReward = (int)missionRank * 100,
                        MoneyReward = (int)missionRank * 50,
                        StatReward = (int)missionRank * 10
                    });
                }
                
                OnMissionsReceived?.Invoke(missions);
                return missions;
            }
            catch (Exception ex)
            {
                OnErrorReceived?.Invoke($"Failed to get missions: {ex.Message}");
                throw;
            }
        }
        
        public async Task<bool> AcceptMission(int missionId)
        {
            if (!isConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }
            
            try
            {
                // Simulate API call
                await Task.Delay(300);
                
                Debug.Log($"Mission {missionId} accepted successfully");
                return true;
            }
            catch (Exception ex)
            {
                OnErrorReceived?.Invoke($"Failed to accept mission: {ex.Message}");
                throw;
            }
        }
        
        public async Task<bool> CompleteMission(int missionId)
        {
            if (!isConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }
            
            try
            {
                // Simulate API call
                await Task.Delay(500);
                
                Debug.Log($"Mission {missionId} completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                OnErrorReceived?.Invoke($"Failed to complete mission: {ex.Message}");
                throw;
            }
        }
        
        // Combat API Methods
        public async Task<BattleGrid> CreateBattle(CreateBattleRequest request)
        {
            if (!isConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }
            
            try
            {
                // Simulate API call
                await Task.Delay(400);
                
                var battle = new BattleGrid
                {
                    Id = UnityEngine.Random.Range(1000, 9999),
                    Type = request.BattleType,
                    MissionId = request.MissionId,
                    Status = BattleStatus.Preparing,
                    CreatedDate = DateTime.UtcNow
                };
                
                return battle;
            }
            catch (Exception ex)
            {
                OnErrorReceived?.Invoke($"Failed to create battle: {ex.Message}");
                throw;
            }
        }
        
        public async Task<bool> JoinBattle(JoinBattleRequest request)
        {
            if (!isConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }
            
            try
            {
                // Simulate API call
                await Task.Delay(300);
                
                Debug.Log($"Joined battle {request.BattleId} successfully");
                return true;
            }
            catch (Exception ex)
            {
                OnErrorReceived?.Invoke($"Failed to join battle: {ex.Message}");
                throw;
            }
        }
        
        public async Task<CombatActionResult> ProcessCombatAction(CombatActionRequest request)
        {
            if (!isConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }
            
            try
            {
                // Simulate API call
                await Task.Delay(200);
                
                var result = new CombatActionResult
                {
                    IsSuccessful = true,
                    DamageDealt = request.ActionType == ActionType.Attack ? UnityEngine.Random.Range(10, 50) : null,
                    HealingDone = request.ActionType == ActionType.Heal ? UnityEngine.Random.Range(20, 80) : null
                };
                
                return result;
            }
            catch (Exception ex)
            {
                OnErrorReceived?.Invoke($"Failed to process combat action: {ex.Message}");
                throw;
            }
        }
        
        public async Task<bool> StartBattle(int battleId)
        {
            if (!isConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }
            
            try
            {
                // Simulate API call
                await Task.Delay(300);
                
                Debug.Log($"Battle {battleId} started successfully");
                return true;
            }
            catch (Exception ex)
            {
                OnErrorReceived?.Invoke($"Failed to start battle: {ex.Message}");
                throw;
            }
        }
        
        public async Task<bool> EndTurn(int battleId, int characterId)
        {
            if (!isConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }
            
            try
            {
                // Simulate API call
                await Task.Delay(200);
                
                Debug.Log($"Turn ended for character {characterId} in battle {battleId}");
                return true;
            }
            catch (Exception ex)
            {
                OnErrorReceived?.Invoke($"Failed to end turn: {ex.Message}");
                throw;
            }
        }
        
        private void OnDestroy()
        {
            DisconnectFromServer();
        }
    }
    
    // Request/Response models
    [System.Serializable]
    public class CreateCharacterRequest
    {
        public string Name;
        public Gender Gender;
        public Village Village;
    }
    
    [System.Serializable]
    public class UpdateCharacterRequest
    {
        public string? Name;
        public Gender? Gender;
    }
    
    [System.Serializable]
    public class TrainingRequest
    {
        public CombatType TrainingType;
    }
    
    [System.Serializable]
    public class TrainingResult
    {
        public CombatType TrainingType;
        public int PointsGained;
        public int Cost;
        public int NewBalance;
    }
    
    [System.Serializable]
    public class CreateBattleRequest
    {
        public int CharacterId;
        public BattleType BattleType;
        public int? MissionId;
    }
    
    [System.Serializable]
    public class JoinBattleRequest
    {
        public int BattleId;
        public int CharacterId;
    }
    
    [System.Serializable]
    public class CombatActionRequest
    {
        public int BattleId;
        public int CharacterId;
        public ActionType ActionType;
        public int APCost;
        public int CPCost;
        public int SPCost;
        public int? TargetCharacterId;
        public int? TargetRow;
        public int? TargetColumn;
        public CombatType? CombatType;
        public Element? Element;
        public int? ItemId;
    }
    
    [System.Serializable]
    public class CombatActionResult
    {
        public bool IsSuccessful;
        public int? DamageDealt;
        public int? HealingDone;
        public string? Message;
    }
}
