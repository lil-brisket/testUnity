using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NinjaMMORPG.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Game Systems")]
        public NetworkManager networkManager;
        public CombatManager combatManager;
        public UIManager uiManager;
        public CharacterManager characterManager;
        public InventoryManager inventoryManager;
        public SocialManager socialManager;
        
        [Header("Game State")]
        public GameState currentGameState = GameState.MainMenu;
        public Character currentCharacter;
        public Village currentVillage;
        public bool isConnectedToServer = false;
        
        [Header("Settings")]
        public float turnTimeLimit = 120f;
        public int maxActionPoints = 100;
        public float regenerationInterval = 1f;
        
        private float lastRegenerationTime;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGameSystems();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Start with main menu
            SetGameState(GameState.MainMenu);
            
            // Initialize regeneration system
            lastRegenerationTime = Time.time;
        }
        
        private void Update()
        {
            // Handle regeneration
            if (currentCharacter != null && !currentCharacter.IsInBattle)
            {
                HandleRegeneration();
            }
            
            // Handle input
            HandleInput();
        }
        
        private void InitializeGameSystems()
        {
            // Ensure all managers exist
            if (networkManager == null)
                networkManager = GetComponent<NetworkManager>();
                
            if (combatManager == null)
                combatManager = GetComponent<CombatManager>();
                
            if (uiManager == null)
                uiManager = GetComponent<UIManager>();
                
            if (characterManager == null)
                characterManager = GetComponent<CharacterManager>();
                
            if (inventoryManager == null)
                inventoryManager = GetComponent<InventoryManager>();
                
            if (socialManager == null)
                socialManager = GetComponent<SocialManager>();
        }
        
        public void SetGameState(GameState newState)
        {
            var previousState = currentGameState;
            currentGameState = newState;
            
            Debug.Log($"Game state changed from {previousState} to {newState}");
            
            // Handle state-specific logic
            switch (newState)
            {
                case GameState.MainMenu:
                    HandleMainMenuState();
                    break;
                case GameState.CharacterCreation:
                    HandleCharacterCreationState();
                    break;
                case GameState.Village:
                    HandleVillageState();
                    break;
                case GameState.Combat:
                    HandleCombatState();
                    break;
                case GameState.Hospital:
                    HandleHospitalState();
                    break;
                case GameState.Overworld:
                    HandleOverworldState();
                    break;
            }
            
            // Notify UI manager
            if (uiManager != null)
                uiManager.OnGameStateChanged(previousState, newState);
        }
        
        private void HandleMainMenuState()
        {
            // Show main menu UI
            if (uiManager != null)
                uiManager.ShowMainMenu();
        }
        
        private void HandleCharacterCreationState()
        {
            // Show character creation UI
            if (uiManager != null)
                uiManager.ShowCharacterCreation();
        }
        
        private void HandleVillageState()
        {
            // Show village UI and join village chat
            if (uiManager != null)
                uiManager.ShowVillage();
                
            if (socialManager != null && currentCharacter != null)
                socialManager.JoinVillageChat(currentCharacter.Village.ToString());
        }
        
        private void HandleCombatState()
        {
            // Show combat UI
            if (uiManager != null)
                uiManager.ShowCombat();
        }
        
        private void HandleHospitalState()
        {
            // Show hospital UI
            if (uiManager != null)
                uiManager.ShowHospital();
        }
        
        private void HandleOverworldState()
        {
            // Show overworld UI
            if (uiManager != null)
                uiManager.ShowOverworld();
        }
        
        private void HandleRegeneration()
        {
            if (Time.time - lastRegenerationTime >= regenerationInterval)
            {
                RegenerateCharacter();
                lastRegenerationTime = Time.time;
            }
        }
        
        private void RegenerateCharacter()
        {
            if (currentCharacter == null) return;
            
            // Regenerate HP
            if (currentCharacter.HP < currentCharacter.MaxHP)
            {
                currentCharacter.HP = Mathf.Min(currentCharacter.MaxHP, 
                    currentCharacter.HP + CalculateRegenerationRate(currentCharacter.HP, currentCharacter.MaxHP));
            }
            
            // Regenerate CP
            if (currentCharacter.CP < currentCharacter.MaxCP)
            {
                currentCharacter.CP = Mathf.Min(currentCharacter.MaxCP, 
                    currentCharacter.CP + CalculateRegenerationRate(currentCharacter.CP, currentCharacter.MaxCP));
            }
            
            // Regenerate SP
            if (currentCharacter.SP < currentCharacter.MaxSP)
            {
                currentCharacter.SP = Mathf.Min(currentCharacter.MaxSP, 
                    currentCharacter.SP + CalculateRegenerationRate(currentCharacter.SP, currentCharacter.MaxSP));
            }
        }
        
        private int CalculateRegenerationRate(int current, int max)
        {
            // Base regeneration rate based on character level and rank
            var baseRate = 1;
            
            if (currentCharacter != null)
            {
                baseRate += currentCharacter.Level / 10;
                baseRate += (int)currentCharacter.Rank;
            }
            
            // Faster regeneration when health is low
            var healthPercentage = (float)current / max;
            if (healthPercentage < 0.25f)
                baseRate *= 2;
            else if (healthPercentage < 0.5f)
                baseRate = (int)(baseRate * 1.5f);
                
            return baseRate;
        }
        
        private void HandleInput()
        {
            // Handle global input shortcuts
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscapeKey();
            }
            
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                HandleTabKey();
            }
        }
        
        private void HandleEscapeKey()
        {
            switch (currentGameState)
            {
                case GameState.Combat:
                    // Show combat menu
                    if (uiManager != null)
                        uiManager.ShowCombatMenu();
                    break;
                case GameState.Village:
                    // Show village menu
                    if (uiManager != null)
                        uiManager.ShowVillageMenu();
                    break;
                default:
                    // Return to previous state or main menu
                    SetGameState(GameState.MainMenu);
                    break;
            }
        }
        
        private void HandleTabKey()
        {
            // Toggle character sheet
            if (uiManager != null)
                uiManager.ToggleCharacterSheet();
        }
        
        public async Task<bool> ConnectToServer()
        {
            if (networkManager != null)
            {
                isConnectedToServer = await networkManager.ConnectToServer();
                return isConnectedToServer;
            }
            return false;
        }
        
        public void DisconnectFromServer()
        {
            if (networkManager != null)
            {
                networkManager.DisconnectFromServer();
                isConnectedToServer = false;
            }
        }
        
        public void SetCurrentCharacter(Character character)
        {
            currentCharacter = character;
            currentVillage = character.Village;
            
            // Update UI
            if (uiManager != null)
                uiManager.UpdateCharacterDisplay(character);
        }
        
        public void EnterCombat(BattleGrid battleGrid)
        {
            if (combatManager != null)
            {
                combatManager.InitializeBattle(battleGrid);
                SetGameState(GameState.Combat);
            }
        }
        
        public void ExitCombat()
        {
            if (combatManager != null)
            {
                combatManager.EndBattle();
                SetGameState(GameState.Village);
            }
        }
        
        public void EnterHospital()
        {
            SetGameState(GameState.Hospital);
        }
        
        public void ExitHospital()
        {
            SetGameState(GameState.Village);
        }
        
        public void TravelToVillage(Village village)
        {
            currentVillage = village;
            if (currentCharacter != null)
            {
                currentCharacter.Village = village;
            }
            
            SetGameState(GameState.Village);
        }
        
        public void TravelToOverworld()
        {
            SetGameState(GameState.Overworld);
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Save game state when app is paused
                SaveGameState();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // Save game state when app loses focus
                SaveGameState();
            }
        }
        
        private void SaveGameState()
        {
            if (currentCharacter != null)
            {
                // Save character data
                PlayerPrefs.SetString("LastCharacterName", currentCharacter.Name);
                PlayerPrefs.SetInt("LastVillage", (int)currentCharacter.Village);
                PlayerPrefs.Save();
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup when game manager is destroyed
            if (Instance == this)
            {
                SaveGameState();
                DisconnectFromServer();
            }
        }
    }
    
    public enum GameState
    {
        MainMenu,
        CharacterCreation,
        Village,
        Combat,
        Hospital,
        Overworld
    }
}
