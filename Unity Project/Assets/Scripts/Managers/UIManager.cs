using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace NinjaMMORPG.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject characterCreationPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject combatPanel;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject characterPanel;
        [SerializeField] private GameObject missionPanel;
        [SerializeField] private GameObject villagePanel;
        [SerializeField] private GameObject clanPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("Main Menu UI")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text versionText;

        [Header("Character Creation UI")]
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_Dropdown genderDropdown;
        [SerializeField] private TMP_Dropdown villageDropdown;
        [SerializeField] private Button createCharacterButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text errorText;

        [Header("Game UI")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text playerRankText;
        [SerializeField] private TMP_Text playerLevelText;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Slider cpSlider;
        [SerializeField] private Slider spSlider;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text cpText;
        [SerializeField] private TMP_Text spText;
        [SerializeField] private TMP_Text ryoText;
        [SerializeField] private TMP_Text villageText;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button characterButton;
        [SerializeField] private Button missionButton;
        [SerializeField] private Button villageButton;
        [SerializeField] private Button clanButton;

        [Header("Combat UI")]
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private TMP_Text actionPointsText;
        [SerializeField] private TMP_Text turnTimerText;
        [SerializeField] private Button moveButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button jutsuButton;
        [SerializeField] private Button healButton;
        [SerializeField] private Button itemButton;
        [SerializeField] private Button fleeButton;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button surrenderButton;
        [SerializeField] private GameObject combatGrid;
        [SerializeField] private GameObject gridTilePrefab;

        [Header("Inventory UI")]
        [SerializeField] private Transform itemContainer;
        [SerializeField] private GameObject itemSlotPrefab;
        [SerializeField] private Button useItemButton;
        [SerializeField] private Button equipItemButton;
        [SerializeField] private Button dropItemButton;
        [SerializeField] private TMP_Text itemDescriptionText;

        [Header("Character UI")]
        [SerializeField] private TMP_Text strengthText;
        [SerializeField] private TMP_Text intelligenceText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text willpowerText;
        [SerializeField] private TMP_Text ninjutsuText;
        [SerializeField] private TMP_Text genjutsuText;
        [SerializeField] private TMP_Text bukijutsuText;
        [SerializeField] private TMP_Text taijutsuText;
        [SerializeField] private TMP_Text experienceText;
        [SerializeField] private Slider experienceSlider;
        [SerializeField] private Button trainButton;

        [Header("Mission UI")]
        [SerializeField] private Transform missionContainer;
        [SerializeField] private GameObject missionItemPrefab;
        [SerializeField] private Button acceptMissionButton;
        [SerializeField] private Button abandonMissionButton;
        [SerializeField] private TMP_Text missionDescriptionText;

        [Header("Village UI")]
        [SerializeField] private TMP_Text villageNameText;
        [SerializeField] private TMP_Text villageDescriptionText;
        [SerializeField] private Button enterBuildingButton;
        [SerializeField] private Button leaveVillageButton;
        [SerializeField] private Transform buildingContainer;
        [SerializeField] private GameObject buildingButtonPrefab;

        [Header("Clan UI")]
        [SerializeField] private TMP_Text clanNameText;
        [SerializeField] private TMP_Text clanLeaderText;
        [SerializeField] private TMP_Text clanMembersText;
        [SerializeField] private Button createClanButton;
        [SerializeField] private Button joinClanButton;
        [SerializeField] private Button leaveClanButton;
        [SerializeField] private Transform memberContainer;
        [SerializeField] private GameObject memberItemPrefab;

        [Header("Settings UI")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;

        [Header("Loading UI")]
        [SerializeField] private Slider loadingProgressSlider;
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private TMP_Text progressText;

        // Current UI state
        private GameObject currentActivePanel;
        private Dictionary<string, GameObject> uiPanels;
        private bool isInitialized = false;

        // Events
        public event Action<string> OnCharacterNameSubmitted;
        public event Action<Gender> OnGenderSelected;
        public event Action<Village> OnVillageSelected;
        public event Action OnCharacterCreationConfirmed;
        public event Action<ActionType> OnCombatActionSelected;
        public event Action OnInventoryOpened;
        public event Action OnCharacterOpened;
        public event Action OnMissionOpened;
        public event Action OnVillageOpened;
        public event Action OnClanOpened;
        public event Action OnSettingsOpened;

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
            InitializeUI();
            SetupEventListeners();
            ShowMainMenu();
        }

        #region Initialization

        private void InitializeUI()
        {
            if (isInitialized) return;

            // Initialize UI panels dictionary
            uiPanels = new Dictionary<string, GameObject>
            {
                { "MainMenu", mainMenuPanel },
                { "CharacterCreation", characterCreationPanel },
                { "Game", gamePanel },
                { "Combat", combatPanel },
                { "Inventory", inventoryPanel },
                { "Character", characterPanel },
                { "Mission", missionPanel },
                { "Village", villagePanel },
                { "Clan", clanPanel },
                { "Settings", settingsPanel },
                { "Loading", loadingPanel }
            };

            // Hide all panels initially
            foreach (var panel in uiPanels.Values)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }

            // Set version text
            if (versionText != null)
            {
                versionText.text = $"Version {Application.version}";
            }

            isInitialized = true;
        }

        private void SetupEventListeners()
        {
            // Main menu buttons
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayButtonClicked);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitButtonClicked);

            // Character creation buttons
            if (createCharacterButton != null)
                createCharacterButton.onClick.AddListener(OnCreateCharacterClicked);
            if (backButton != null)
                backButton.onClick.AddListener(OnBackButtonClicked);

            // Game UI buttons
            if (inventoryButton != null)
                inventoryButton.onClick.AddListener(OnInventoryButtonClicked);
            if (characterButton != null)
                characterButton.onClick.AddListener(OnCharacterButtonClicked);
            if (missionButton != null)
                missionButton.onClick.AddListener(OnMissionButtonClicked);
            if (villageButton != null)
                villageButton.onClick.AddListener(OnVillageButtonClicked);
            if (clanButton != null)
                clanButton.onClick.AddListener(OnClanButtonClicked);

            // Combat UI buttons
            if (moveButton != null)
                moveButton.onClick.AddListener(() => OnCombatActionSelected?.Invoke(ActionType.Move));
            if (attackButton != null)
                attackButton.onClick.AddListener(() => OnCombatActionSelected?.Invoke(ActionType.Attack));
            if (jutsuButton != null)
                jutsuButton.onClick.AddListener(() => OnCombatActionSelected?.Invoke(ActionType.Jutsu));
            if (healButton != null)
                healButton.onClick.AddListener(() => OnCombatActionSelected?.Invoke(ActionType.Heal));
            if (itemButton != null)
                itemButton.onClick.AddListener(() => OnCombatActionSelected?.Invoke(ActionType.Item));
            if (fleeButton != null)
                fleeButton.onClick.AddListener(() => OnCombatActionSelected?.Invoke(ActionType.Flee));
            if (readyButton != null)
                readyButton.onClick.AddListener(OnReadyButtonClicked);
            if (surrenderButton != null)
                surrenderButton.onClick.AddListener(OnSurrenderButtonClicked);

            // Settings UI
            if (applyButton != null)
                applyButton.onClick.AddListener(OnApplySettingsClicked);
            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetSettingsClicked);
        }

        #endregion

        #region Panel Management

        public void ShowPanel(string panelName)
        {
            if (!uiPanels.ContainsKey(panelName))
            {
                Debug.LogWarning($"Panel '{panelName}' not found!");
                return;
            }

            // Hide current panel
            if (currentActivePanel != null)
            {
                currentActivePanel.SetActive(false);
            }

            // Show new panel
            currentActivePanel = uiPanels[panelName];
            currentActivePanel.SetActive(true);

            // Animate panel appearance
            AnimatePanelIn(currentActivePanel);
        }

        public void HidePanel(string panelName)
        {
            if (uiPanels.ContainsKey(panelName))
            {
                AnimatePanelOut(uiPanels[panelName], () => uiPanels[panelName].SetActive(false));
            }
        }

        private void AnimatePanelIn(GameObject panel)
        {
            if (panel == null) return;

            // Reset scale and alpha
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            panel.transform.localScale = Vector3.one * 0.8f;

            // Animate in
            canvasGroup.DOFade(1f, 0.3f);
            panel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }

        private void AnimatePanelOut(GameObject panel, Action onComplete = null)
        {
            if (panel == null) return;

            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }

            // Animate out
            canvasGroup.DOFade(0f, 0.2f);
            panel.transform.DOScale(Vector3.one * 0.8f, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => onComplete?.Invoke());
        }

        #endregion

        #region Main Menu

        public void ShowMainMenu()
        {
            ShowPanel("MainMenu");
        }

        private void OnPlayButtonClicked()
        {
            if (GameManager.Instance.HasCharacter)
            {
                ShowGameUI();
            }
            else
            {
                ShowCharacterCreation();
            }
        }

        private void OnSettingsButtonClicked()
        {
            ShowPanel("Settings");
        }

        private void OnQuitButtonClicked()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        #endregion

        #region Character Creation

        public void ShowCharacterCreation()
        {
            ShowPanel("CharacterCreation");
            InitializeCharacterCreationUI();
        }

        private void InitializeCharacterCreationUI()
        {
            // Initialize gender dropdown
            if (genderDropdown != null)
            {
                genderDropdown.ClearOptions();
                genderDropdown.AddOptions(new List<string> { "Male", "Female", "Non-Binary" });
            }

            // Initialize village dropdown
            if (villageDropdown != null)
            {
                villageDropdown.ClearOptions();
                villageDropdown.AddOptions(new List<string> 
                { 
                    "Hidden Leaf Village", 
                    "Hidden Stone Village", 
                    "Hidden Mist Village", 
                    "Hidden Sand Village", 
                    "Hidden Cloud Village" 
                });
            }

            // Clear error text
            if (errorText != null)
            {
                errorText.text = "";
            }
        }

        private void OnCreateCharacterClicked()
        {
            if (string.IsNullOrEmpty(nameInput.text))
            {
                ShowError("Please enter a character name.");
                return;
            }

            // Validate name length
            if (nameInput.text.Length < 3 || nameInput.text.Length > 20)
            {
                ShowError("Character name must be between 3 and 20 characters.");
                return;
            }

            // Get selected values
            Gender selectedGender = (Gender)genderDropdown.value;
            Village selectedVillage = (Village)villageDropdown.value;

            // Trigger character creation events
            OnCharacterNameSubmitted?.Invoke(nameInput.text);
            OnGenderSelected?.Invoke(selectedGender);
            OnVillageSelected?.Invoke(selectedVillage);
            OnCharacterCreationConfirmed?.Invoke();

            // Hide character creation and show loading
            ShowLoading("Creating character...");
        }

        private void OnBackButtonClicked()
        {
            ShowMainMenu();
        }

        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.color = Color.red;
            }
        }

        #endregion

        #region Game UI

        public void ShowGameUI()
        {
            ShowPanel("Game");
            UpdateGameUI();
        }

        public void UpdateGameUI()
        {
            var character = GameManager.Instance.CurrentCharacter;
            if (character == null) return;

            // Update character info
            if (playerNameText != null)
                playerNameText.text = character.Name;
            if (playerRankText != null)
                playerRankText.text = character.Rank.ToString();
            if (playerLevelText != null)
                playerLevelText.text = $"Level {character.Level}";

            // Update resource bars
            if (hpSlider != null)
            {
                hpSlider.value = (float)character.CurrentHP / character.MaxHP;
                if (hpText != null)
                    hpText.text = $"{character.CurrentHP}/{character.MaxHP}";
            }

            if (cpSlider != null)
            {
                cpSlider.value = (float)character.CurrentCP / character.MaxCP;
                if (cpText != null)
                    cpText.text = $"{character.CurrentCP}/{character.MaxCP}";
            }

            if (spSlider != null)
            {
                spSlider.value = (float)character.CurrentSP / character.MaxSP;
                if (spText != null)
                    spText.text = $"{character.CurrentSP}/{character.MaxSP}";
            }

            // Update other info
            if (ryoText != null)
                ryoText.text = $"{character.PocketRyo:N0} Ryo";
            if (villageText != null)
                villageText.text = character.Village.ToString();
        }

        private void OnInventoryButtonClicked()
        {
            OnInventoryOpened?.Invoke();
            ShowPanel("Inventory");
        }

        private void OnCharacterButtonClicked()
        {
            OnCharacterOpened?.Invoke();
            ShowPanel("Character");
            UpdateCharacterUI();
        }

        private void OnMissionButtonClicked()
        {
            OnMissionOpened?.Invoke();
            ShowPanel("Mission");
            UpdateMissionUI();
        }

        private void OnVillageButtonClicked()
        {
            OnVillageOpened?.Invoke();
            ShowPanel("Village");
            UpdateVillageUI();
        }

        private void OnClanButtonClicked()
        {
            OnClanOpened?.Invoke();
            ShowPanel("Clan");
            UpdateClanUI();
        }

        #endregion

        #region Combat UI

        public void ShowCombatUI()
        {
            ShowPanel("Combat");
            UpdateCombatUI();
        }

        public void UpdateCombatUI()
        {
            if (CombatManager.Instance == null) return;

            // Update turn info
            if (turnText != null)
                turnText.text = $"Turn {CombatManager.Instance.CurrentTurn}";
            if (actionPointsText != null)
                actionPointsText.text = $"AP: {CombatManager.Instance.CurrentPlayer?.ActionPoints ?? 0}";
            if (turnTimerText != null)
                turnTimerText.text = $"Time: {CombatManager.Instance.TurnTimer:F1}s";

            // Update button states
            bool isPlayerTurn = CombatManager.Instance.CurrentPlayer?.Character.Id == GameManager.Instance.CurrentCharacter?.Id;
            if (moveButton != null) moveButton.interactable = isPlayerTurn;
            if (attackButton != null) attackButton.interactable = isPlayerTurn;
            if (jutsuButton != null) jutsuButton.interactable = isPlayerTurn;
            if (healButton != null) healButton.interactable = isPlayerTurn;
            if (itemButton != null) itemButton.interactable = isPlayerTurn;
            if (fleeButton != null) fleeButton.interactable = isPlayerTurn;
        }

        public void UpdateCombatGrid()
        {
            if (CombatManager.Instance == null || combatGrid == null) return;

            // Clear existing grid
            foreach (Transform child in combatGrid.transform)
            {
                Destroy(child.gameObject);
            }

            var battleGrid = CombatManager.Instance.BattleGrid;
            if (battleGrid == null) return;

            // Create grid tiles
            for (int x = 0; x < battleGrid.GetLength(0); x++)
            {
                for (int y = 0; y < battleGrid.GetLength(1); y++)
                {
                    var tile = Instantiate(gridTilePrefab, combatGrid.transform);
                    var tileComponent = tile.GetComponent<CombatGridTileUI>();
                    if (tileComponent != null)
                    {
                        tileComponent.Initialize(new Vector2Int(x, y), battleGrid[x, y]);
                    }
                }
            }
        }

        private void OnReadyButtonClicked()
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.ReadyUp();
            }
        }

        private void OnSurrenderButtonClicked()
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.Surrender();
            }
        }

        #endregion

        #region Character UI

        private void UpdateCharacterUI()
        {
            var character = GameManager.Instance.CurrentCharacter;
            if (character == null) return;

            // Update stats
            if (strengthText != null)
                strengthText.text = $"Strength: {character.Strength:N0}";
            if (intelligenceText != null)
                intelligenceText.text = $"Intelligence: {character.Intelligence:N0}";
            if (speedText != null)
                speedText.text = $"Speed: {character.Speed:N0}";
            if (willpowerText != null)
                willpowerText.text = $"Willpower: {character.Willpower:N0}";

            if (ninjutsuText != null)
                ninjutsuText.text = $"Ninjutsu: {character.Ninjutsu:N0}";
            if (genjutsuText != null)
                genjutsuText.text = $"Genjutsu: {character.Genjutsu:N0}";
            if (bukijutsuText != null)
                bukijutsuText.text = $"Bukijutsu: {character.Bukijutsu:N0}";
            if (taijutsuText != null)
                taijutsuText.text = $"Taijutsu: {character.Taijutsu:N0}";

            // Update experience
            if (experienceText != null)
                experienceText.text = $"Experience: {character.Experience:N0}";
            if (experienceSlider != null)
            {
                float expProgress = (float)(character.Experience % 1000) / 1000f;
                experienceSlider.value = expProgress;
            }
        }

        #endregion

        #region Mission UI

        private void UpdateMissionUI()
        {
            // This would populate the mission list
            // For now, just show a placeholder
            if (missionDescriptionText != null)
            {
                missionDescriptionText.text = "Available missions will appear here.";
            }
        }

        #endregion

        #region Village UI

        private void UpdateVillageUI()
        {
            var character = GameManager.Instance.CurrentCharacter;
            if (character == null) return;

            if (villageNameText != null)
                villageNameText.text = character.Village.ToString();
            if (villageDescriptionText != null)
                villageDescriptionText.text = GetVillageDescription(character.Village);
        }

        private string GetVillageDescription(Village village)
        {
            return village switch
            {
                Village.HiddenLeaf => "A village hidden in the leaves, known for its strong ninja tradition and forest surroundings.",
                Village.HiddenStone => "A village hidden in the mountains, famous for its earth-based jutsu and rocky terrain.",
                Village.HiddenMist => "A village hidden in the mist, known for its water techniques and coastal location.",
                Village.HiddenSand => "A village hidden in the desert, renowned for its wind jutsu and sandy environment.",
                Village.HiddenCloud => "A village hidden in the clouds, famous for its lightning techniques and mountainous peaks.",
                _ => "An unknown village."
            };
        }

        #endregion

        #region Clan UI

        private void UpdateClanUI()
        {
            var character = GameManager.Instance.CurrentCharacter;
            if (character == null) return;

            // This would show clan information
            // For now, just show basic info
            if (clanNameText != null)
                clanNameText.text = "No Clan";
            if (clanLeaderText != null)
                clanLeaderText.text = "Leader: None";
            if (clanMembersText != null)
                clanMembersText.text = "Members: 0";
        }

        #endregion

        #region Settings UI

        private void OnApplySettingsClicked()
        {
            // Apply settings
            if (musicVolumeSlider != null)
                AudioManager.Instance?.SetMusicVolume(musicVolumeSlider.value);
            if (sfxVolumeSlider != null)
                AudioManager.Instance?.SetSFXVolume(sfxVolumeSlider.value);
            if (fullscreenToggle != null)
                Screen.fullScreen = fullscreenToggle.isOn;
            if (qualityDropdown != null)
                QualitySettings.SetQualityLevel(qualityDropdown.value);

            ShowGameUI();
        }

        private void OnResetSettingsClicked()
        {
            // Reset to default settings
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = 0.7f;
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = 1.0f;
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = true;
            if (qualityDropdown != null)
                qualityDropdown.value = QualitySettings.GetQualityLevel();
        }

        #endregion

        #region Loading UI

        public void ShowLoading(string message = "Loading...")
        {
            ShowPanel("Loading");
            if (loadingText != null)
                loadingText.text = message;
            if (loadingProgressSlider != null)
                loadingProgressSlider.value = 0f;
        }

        public void UpdateLoadingProgress(float progress, string message = null)
        {
            if (loadingProgressSlider != null)
                loadingProgressSlider.value = progress;
            if (progressText != null)
                progressText.text = $"{progress * 100:F0}%";
            if (message != null && loadingText != null)
                loadingText.text = message;
        }

        public void HideLoading()
        {
            HidePanel("Loading");
        }

        #endregion

        #region Public Interface

        public void ShowNotification(string message, float duration = 3f)
        {
            // Create a temporary notification
            StartCoroutine(ShowTemporaryNotification(message, duration));
        }

        private System.Collections.IEnumerator ShowTemporaryNotification(string message, float duration)
        {
            // This would create and show a notification UI element
            Debug.Log($"Notification: {message}");
            yield return new WaitForSeconds(duration);
        }

        public void UpdateCombatTimer(float timeRemaining)
        {
            if (turnTimerText != null)
            {
                turnTimerText.text = $"Time: {timeRemaining:F1}s";
            }
        }

        public void UpdateActionPoints(int actionPoints)
        {
            if (actionPointsText != null)
            {
                actionPointsText.text = $"AP: {actionPoints}";
            }
        }

        #endregion
    }

    #region Supporting Classes

    public class CombatGridTileUI : MonoBehaviour
    {
        [SerializeField] private Image tileImage;
        [SerializeField] private Button tileButton;
        [SerializeField] private TMP_Text occupantText;

        private Vector2Int position;
        private CombatGridTile tileData;

        public void Initialize(Vector2Int pos, CombatGridTile data)
        {
            position = pos;
            tileData = data;

            if (tileButton != null)
            {
                tileButton.onClick.AddListener(OnTileClicked);
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (tileData == null) return;

            // Update tile appearance based on state
            if (tileData.IsOccupied)
            {
                tileImage.color = Color.red;
                if (occupantText != null)
                    occupantText.text = "X";
            }
            else
            {
                tileImage.color = Color.white;
                if (occupantText != null)
                    occupantText.text = "";
            }

            // Highlight if it's a valid move position
            if (CombatManager.Instance != null && 
                CombatManager.Instance.ValidMovePositions.Contains(position))
            {
                tileImage.color = Color.green;
            }
        }

        private void OnTileClicked()
        {
            // Handle tile click for movement or targeting
            if (CombatManager.Instance != null && CombatManager.Instance.IsInCombat)
            {
                // This would trigger movement or targeting logic
                Debug.Log($"Tile clicked at {position}");
            }
        }
    }

    #endregion
}
