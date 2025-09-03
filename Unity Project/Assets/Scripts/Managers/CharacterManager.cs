using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace NinjaMMORPG.Managers
{
    public class CharacterManager : MonoBehaviour
    {
        public static CharacterManager Instance { get; private set; }

        [Header("Character Settings")]
        [SerializeField] private float regenerationInterval = 1f; // seconds
        [SerializeField] private int baseRegenerationRate = 1;
        [SerializeField] private float trainingCooldown = 5f; // seconds

        // Character data
        private Character currentCharacter;
        private Dictionary<string, Character> cachedCharacters = new Dictionary<string, Character>();

        // Training state
        private Dictionary<StatType, DateTime> lastTrainingTimes = new Dictionary<StatType, DateTime>();
        private bool isTraining = false;

        // Regeneration
        private float regenerationTimer = 0f;
        private bool regenerationEnabled = true;

        // Events
        public event Action<Character> OnCharacterLoaded;
        public event Action<Character> OnCharacterUpdated;
        public event Action<Character> OnCharacterDeleted;
        public event Action<StatType, int> OnStatTrained;
        public event Action<int> OnExperienceGained;
        public event Action<Rank> OnRankAdvanced;
        public event Action OnRegenerationTick;

        // Stat training costs and gains
        private readonly Dictionary<StatType, (int cost, int gain)> statTrainingData = new Dictionary<StatType, (int cost, int gain)>
        {
            { StatType.Strength, (10, 5) },
            { StatType.Intelligence, (10, 5) },
            { StatType.Speed, (10, 5) },
            { StatType.Willpower, (10, 5) },
            { StatType.Ninjutsu, (20, 10) },
            { StatType.Genjutsu, (20, 10) },
            { StatType.Bukijutsu, (20, 10) },
            { StatType.Taijutsu, (20, 10) }
        };

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
            InitializeTrainingTimes();
        }

        private void Update()
        {
            if (regenerationEnabled && currentCharacter != null)
            {
                UpdateRegeneration();
            }
        }

        #region Initialization

        private void InitializeTrainingTimes()
        {
            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            {
                lastTrainingTimes[statType] = DateTime.MinValue;
            }
        }

        #endregion

        #region Character Management

        public async Task<bool> CreateCharacter(string name, Gender gender, Village village)
        {
            try
            {
                UIManager.Instance.ShowLoading("Creating character...");

                // Create character data
                var characterData = new CharacterData
                {
                    Name = name,
                    Gender = gender,
                    Village = village,
                    // Stats start at 1 as per game design
                    Strength = 1,
                    Intelligence = 1,
                    Speed = 1,
                    Willpower = 1,
                    Ninjutsu = 1,
                    Genjutsu = 1,
                    Bukijutsu = 1,
                    Taijutsu = 1,
                    // Resources start at max
                    CurrentHP = 100,
                    MaxHP = 100,
                    CurrentCP = 100,
                    MaxCP = 100,
                    CurrentSP = 100,
                    MaxSP = 100,
                    // Starting money
                    PocketRyo = 1000,
                    BankRyo = 0,
                    // Progression
                    Level = 1,
                    Experience = 0,
                    Rank = Rank.Student,
                    MedNinRank = MedNinRank.Novice,
                    // Village and location
                    VillageRank = 1,
                    OutlawRank = 0,
                    CurrentLocation = village.ToString(),
                    // Timestamps
                    CreatedAt = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow
                };

                // Send to backend (simulated for now)
                var success = await NetworkManager.Instance.CreateCharacter(characterData);
                
                if (success)
                {
                    // Load the created character
                    await LoadCharacter(name);
                    UIManager.Instance.HideLoading();
                    return true;
                }
                else
                {
                    UIManager.Instance.HideLoading();
                    UIManager.Instance.ShowNotification("Failed to create character. Please try again.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating character: {ex.Message}");
                UIManager.Instance.HideLoading();
                UIManager.Instance.ShowNotification("An error occurred while creating your character.");
                return false;
            }
        }

        public async Task<bool> LoadCharacter(string characterName)
        {
            try
            {
                UIManager.Instance.ShowLoading("Loading character...");

                // Try to load from cache first
                if (cachedCharacters.ContainsKey(characterName))
                {
                    currentCharacter = cachedCharacters[characterName];
                    OnCharacterLoaded?.Invoke(currentCharacter);
                    UIManager.Instance.HideLoading();
                    return true;
                }

                // Load from backend
                var characterData = await NetworkManager.Instance.GetCharacter(characterName);
                
                if (characterData != null)
                {
                    currentCharacter = new Character(characterData);
                    cachedCharacters[characterName] = currentCharacter;
                    
                    OnCharacterLoaded?.Invoke(currentCharacter);
                    UIManager.Instance.HideLoading();
                    return true;
                }
                else
                {
                    UIManager.Instance.HideLoading();
                    UIManager.Instance.ShowNotification("Character not found.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading character: {ex.Message}");
                UIManager.Instance.HideLoading();
                UIManager.Instance.ShowNotification("An error occurred while loading your character.");
                return false;
            }
        }

        public async Task<bool> SaveCharacter()
        {
            if (currentCharacter == null) return false;

            try
            {
                var characterData = currentCharacter.ToCharacterData();
                var success = await NetworkManager.Instance.UpdateCharacter(characterData);
                
                if (success)
                {
                    // Update cache
                    cachedCharacters[currentCharacter.Name] = currentCharacter;
                    OnCharacterUpdated?.Invoke(currentCharacter);
                    return true;
                }
                else
                {
                    UIManager.Instance.ShowNotification("Failed to save character changes.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving character: {ex.Message}");
                UIManager.Instance.ShowNotification("An error occurred while saving your character.");
                return false;
            }
        }

        public async Task<bool> DeleteCharacter(string characterName)
        {
            try
            {
                var success = await NetworkManager.Instance.DeleteCharacter(characterName);
                
                if (success)
                {
                    // Remove from cache
                    if (cachedCharacters.ContainsKey(characterName))
                    {
                        cachedCharacters.Remove(characterName);
                    }

                    // Clear current character if it's the deleted one
                    if (currentCharacter?.Name == characterName)
                    {
                        currentCharacter = null;
                    }

                    OnCharacterDeleted?.Invoke(null);
                    return true;
                }
                else
                {
                    UIManager.Instance.ShowNotification("Failed to delete character.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error deleting character: {ex.Message}");
                UIManager.Instance.ShowNotification("An error occurred while deleting your character.");
                return false;
            }
        }

        #endregion

        #region Character Progression

        public async Task<bool> TrainStat(StatType statType)
        {
            if (currentCharacter == null || isTraining)
            {
                UIManager.Instance.ShowNotification("Cannot train right now.");
                return false;
            }

            // Check training cooldown
            if (DateTime.UtcNow - lastTrainingTimes[statType] < TimeSpan.FromSeconds(trainingCooldown))
            {
                var remainingTime = trainingCooldown - (float)(DateTime.UtcNow - lastTrainingTimes[statType]).TotalSeconds;
                UIManager.Instance.ShowNotification($"Training cooldown: {remainingTime:F1}s remaining");
                return false;
            }

            // Check if stat can be trained (not at max)
            if (!CanTrainStat(statType))
            {
                UIManager.Instance.ShowNotification($"{statType} is already at maximum level!");
                return false;
            }

            // Get training cost and gain
            if (!statTrainingData.TryGetValue(statType, out var trainingInfo))
            {
                UIManager.Instance.ShowNotification("Invalid stat type for training.");
                return false;
            }

            var (cost, gain) = trainingInfo;

            // Check if player has enough CP
            if (currentCharacter.CurrentCP < cost)
            {
                UIManager.Instance.ShowNotification($"Not enough CP! Required: {cost}, Available: {currentCharacter.CurrentCP}");
                return false;
            }

            try
            {
                isTraining = true;
                UIManager.Instance.ShowLoading("Training...");

                // Simulate training time
                await Task.Delay(1000);

                // Apply training
                currentCharacter.ConsumeCP(cost);
                IncreaseStat(statType, gain);
                lastTrainingTimes[statType] = DateTime.UtcNow;

                // Check for rank advancement
                CheckRankAdvancement();

                // Save character
                await SaveCharacter();

                UIManager.Instance.HideLoading();
                UIManager.Instance.ShowNotification($"{statType} trained! +{gain} gained.");

                OnStatTrained?.Invoke(statType, gain);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error training stat: {ex.Message}");
                UIManager.Instance.HideLoading();
                UIManager.Instance.ShowNotification("An error occurred during training.");
                return false;
            }
            finally
            {
                isTraining = false;
            }
        }

        private bool CanTrainStat(StatType statType)
        {
            if (currentCharacter == null) return false;

            var currentValue = GetStatValue(statType);
            var maxValue = GetStatMaxValue(statType);

            return currentValue < maxValue;
        }

        private int GetStatValue(StatType statType)
        {
            return statType switch
            {
                StatType.Strength => currentCharacter.Strength,
                StatType.Intelligence => currentCharacter.Intelligence,
                StatType.Speed => currentCharacter.Speed,
                StatType.Willpower => currentCharacter.Willpower,
                StatType.Ninjutsu => currentCharacter.Ninjutsu,
                StatType.Genjutsu => currentCharacter.Genjutsu,
                StatType.Bukijutsu => currentCharacter.Bukijutsu,
                StatType.Taijutsu => currentCharacter.Taijutsu,
                _ => 0
            };
        }

        private int GetStatMaxValue(StatType statType)
        {
            // Core stats max at 250,000, combat stats max at 500,000
            return statType switch
            {
                StatType.Strength or StatType.Intelligence or StatType.Speed or StatType.Willpower => 250000,
                StatType.Ninjutsu or StatType.Genjutsu or StatType.Bukijutsu or StatType.Taijutsu => 500000,
                _ => 0
            };
        }

        private void IncreaseStat(StatType statType, int amount)
        {
            switch (statType)
            {
                case StatType.Strength:
                    currentCharacter.Strength = Mathf.Min(currentCharacter.Strength + amount, GetStatMaxValue(statType));
                    break;
                case StatType.Intelligence:
                    currentCharacter.Intelligence = Mathf.Min(currentCharacter.Intelligence + amount, GetStatMaxValue(statType));
                    break;
                case StatType.Speed:
                    currentCharacter.Speed = Mathf.Min(currentCharacter.Speed + amount, GetStatMaxValue(statType));
                    break;
                case StatType.Willpower:
                    currentCharacter.Willpower = Mathf.Min(currentCharacter.Willpower + amount, GetStatMaxValue(statType));
                    break;
                case StatType.Ninjutsu:
                    currentCharacter.Ninjutsu = Mathf.Min(currentCharacter.Ninjutsu + amount, GetStatMaxValue(statType));
                    break;
                case StatType.Genjutsu:
                    currentCharacter.Genjutsu = Mathf.Min(currentCharacter.Genjutsu + amount, GetStatMaxValue(statType));
                    break;
                case StatType.Bukijutsu:
                    currentCharacter.Bukijutsu = Mathf.Min(currentCharacter.Bukijutsu + amount, GetStatMaxValue(statType));
                    break;
                case StatType.Taijutsu:
                    currentCharacter.Taijutsu = Mathf.Min(currentCharacter.Taijutsu + amount, GetStatMaxValue(statType));
                    break;
            }
        }

        public void GainExperience(int amount)
        {
            if (currentCharacter == null) return;

            currentCharacter.GainExperience(amount);
            OnExperienceGained?.Invoke(amount);

            // Check for rank advancement
            CheckRankAdvancement();

            // Save character
            _ = SaveCharacter();
        }

        private void CheckRankAdvancement()
        {
            if (currentCharacter == null) return;

            var newRank = CalculateRank(currentCharacter.Experience);
            
            if (newRank != currentCharacter.Rank)
            {
                var oldRank = currentCharacter.Rank;
                currentCharacter.Rank = newRank;
                
                OnRankAdvanced?.Invoke(newRank);
                UIManager.Instance.ShowNotification($"Rank advanced from {oldRank} to {newRank}!");
                
                // Apply rank bonuses
                ApplyRankBonuses(newRank);
            }
        }

        private Rank CalculateRank(int experience)
        {
            // Rank advancement based on experience thresholds
            return experience switch
            {
                >= 10000 => Rank.SpecialJounin,
                >= 5000 => Rank.Jounin,
                >= 2000 => Rank.Chunin,
                >= 500 => Rank.Genin,
                _ => Rank.Student
            };
        }

        private void ApplyRankBonuses(Rank newRank)
        {
            // Apply stat bonuses based on new rank
            switch (newRank)
            {
                case Rank.Genin:
                    // Small stat bonus
                    currentCharacter.Strength += 10;
                    currentCharacter.Intelligence += 10;
                    currentCharacter.Speed += 10;
                    currentCharacter.Willpower += 10;
                    break;
                case Rank.Chunin:
                    // Medium stat bonus
                    currentCharacter.Strength += 25;
                    currentCharacter.Intelligence += 25;
                    currentCharacter.Speed += 25;
                    currentCharacter.Willpower += 25;
                    break;
                case Rank.Jounin:
                    // Large stat bonus
                    currentCharacter.Strength += 50;
                    currentCharacter.Intelligence += 50;
                    currentCharacter.Speed += 50;
                    currentCharacter.Willpower += 50;
                    break;
                case Rank.SpecialJounin:
                    // Major stat bonus
                    currentCharacter.Strength += 100;
                    currentCharacter.Intelligence += 100;
                    currentCharacter.Speed += 100;
                    currentCharacter.Willpower += 100;
                    break;
            }
        }

        #endregion

        #region Regeneration System

        private void UpdateRegeneration()
        {
            regenerationTimer += Time.deltaTime;

            if (regenerationTimer >= regenerationInterval)
            {
                regenerationTimer = 0f;
                ProcessRegeneration();
            }
        }

        private void ProcessRegeneration()
        {
            if (currentCharacter == null) return;

            var regenerationRate = CalculateRegenerationRate();

            // Regenerate HP
            if (currentCharacter.CurrentHP < currentCharacter.MaxHP)
            {
                currentCharacter.CurrentHP = Mathf.Min(currentCharacter.CurrentHP + regenerationRate, currentCharacter.MaxHP);
            }

            // Regenerate CP
            if (currentCharacter.CurrentCP < currentCharacter.MaxCP)
            {
                currentCharacter.CurrentCP = Mathf.Min(currentCharacter.CurrentCP + regenerationRate, currentCharacter.MaxCP);
            }

            // Regenerate SP
            if (currentCharacter.CurrentSP < currentCharacter.MaxSP)
            {
                currentCharacter.CurrentSP = Mathf.Min(currentCharacter.CurrentSP + regenerationRate, currentCharacter.MaxSP);
            }

            OnRegenerationTick?.Invoke();
        }

        private int CalculateRegenerationRate()
        {
            if (currentCharacter == null) return baseRegenerationRate;

            // Base regeneration rate
            var rate = baseRegenerationRate;

            // Intelligence bonus to regeneration
            var intBonus = currentCharacter.Intelligence / 10000; // Every 10k intelligence adds +1 regeneration
            rate += intBonus;

            // Village bonus (some villages have better regeneration)
            if (currentCharacter.Village == Village.HiddenLeaf)
            {
                rate += 1; // Leaf village bonus
            }

            return rate;
        }

        public void SetRegenerationEnabled(bool enabled)
        {
            regenerationEnabled = enabled;
            if (!enabled)
            {
                regenerationTimer = 0f;
            }
        }

        #endregion

        #region Character Queries

        public Character GetCurrentCharacter()
        {
            return currentCharacter;
        }

        public List<Character> GetAllCharacters()
        {
            return new List<Character>(cachedCharacters.Values);
        }

        public Character GetCharacter(string characterName)
        {
            return cachedCharacters.TryGetValue(characterName, out var character) ? character : null;
        }

        public bool HasCharacter(string characterName)
        {
            return cachedCharacters.ContainsKey(characterName);
        }

        public int GetCharacterCount()
        {
            return cachedCharacters.Count;
        }

        #endregion

        #region Utility Methods

        public float GetTrainingCooldownRemaining(StatType statType)
        {
            if (!lastTrainingTimes.ContainsKey(statType))
                return 0f;

            var timeSinceLastTraining = DateTime.UtcNow - lastTrainingTimes[statType];
            var remaining = trainingCooldown - (float)timeSinceLastTraining.TotalSeconds;
            
            return Mathf.Max(0f, remaining);
        }

        public bool IsStatAtMax(StatType statType)
        {
            if (currentCharacter == null) return false;
            
            var currentValue = GetStatValue(statType);
            var maxValue = GetStatMaxValue(statType);
            
            return currentValue >= maxValue;
        }

        public string GetStatProgress(StatType statType)
        {
            if (currentCharacter == null) return "0%";
            
            var currentValue = GetStatValue(statType);
            var maxValue = GetStatMaxValue(statType);
            var percentage = (float)currentValue / maxValue * 100f;
            
            return $"{percentage:F1}%";
        }

        public void ResetTrainingCooldowns()
        {
            foreach (var statType in lastTrainingTimes.Keys)
            {
                lastTrainingTimes[statType] = DateTime.MinValue;
            }
        }

        #endregion

        #region Public Interface

        public bool IsTraining => isTraining;
        public bool RegenerationEnabled => regenerationEnabled;
        public float RegenerationInterval => regenerationInterval;
        public int BaseRegenerationRate => baseRegenerationRate;
        public float TrainingCooldown => trainingCooldown;

        public async Task<bool> RefreshCharacterData()
        {
            if (currentCharacter == null) return false;

            return await LoadCharacter(currentCharacter.Name);
        }

        public void ClearCache()
        {
            cachedCharacters.Clear();
            currentCharacter = null;
        }

        #endregion
    }

    #region Supporting Enums

    public enum StatType
    {
        Strength,
        Intelligence,
        Speed,
        Willpower,
        Ninjutsu,
        Genjutsu,
        Bukijutsu,
        Taijutsu
    }

    #endregion
}
