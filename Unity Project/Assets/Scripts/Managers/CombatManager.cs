using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;

namespace NinjaMMORPG.Managers
{
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [Header("Combat Settings")]
        [SerializeField] private int actionPointsPerTurn = 100;
        [SerializeField] private float turnTimeLimit = 30f;
        [SerializeField] private int gridWidth = 5;
        [SerializeField] private int gridHeight = 8;

        [Header("Combat State")]
        [SerializeField] private bool isInCombat = false;
        [SerializeField] private BattleType currentBattleType;
        [SerializeField] private BattleStatus battleStatus;
        [SerializeField] private int currentTurn = 0;
        [SerializeField] private float turnTimer = 0f;

        // Combat participants
        private List<CombatParticipant> participants = new List<CombatParticipant>();
        private CombatParticipant currentPlayer;
        private int currentPlayerIndex = 0;

        // Grid system
        private CombatGridTile[,] battleGrid;
        private Vector2Int playerPosition;
        private List<Vector2Int> validMovePositions = new List<Vector2Int>();

        // Events
        public event Action<CombatParticipant> OnTurnChanged;
        public event Action<CombatAction> OnActionPerformed;
        public event Action<BattleStatus> OnBattleStatusChanged;
        public event Action<float> OnTurnTimerChanged;
        public event Action<List<Vector2Int>> OnValidMovesUpdated;

        // Combat actions
        private Queue<CombatAction> actionQueue = new Queue<CombatAction>();
        private bool isProcessingActions = false;

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
            InitializeBattleGrid();
        }

        private void Update()
        {
            if (isInCombat && battleStatus == BattleStatus.InProgress)
            {
                UpdateTurnTimer();
            }
        }

        #region Battle Initialization

        public void StartBattle(BattleType battleType, List<Character> characters, Vector2Int playerStartPos)
        {
            if (isInCombat)
            {
                Debug.LogWarning("Already in combat! Cannot start new battle.");
                return;
            }

            currentBattleType = battleType;
            battleStatus = BattleStatus.InProgress;
            currentTurn = 0;
            turnTimer = turnTimeLimit;
            isInCombat = true;

            // Initialize participants
            participants.Clear();
            for (int i = 0; i < characters.Count; i++)
            {
                var participant = new CombatParticipant
                {
                    Character = characters[i],
                    Position = i == 0 ? playerStartPos : GetRandomPosition(),
                    ActionPoints = actionPointsPerTurn,
                    IsReady = false
                };
                participants.Add(participant);
            }

            currentPlayer = participants[0];
            currentPlayerIndex = 0;
            playerPosition = playerStartPos;

            // Initialize grid
            InitializeBattleGrid();
            PlaceParticipantsOnGrid();

            // Start first turn
            StartNewTurn();

            OnBattleStatusChanged?.Invoke(battleStatus);
            Debug.Log($"Battle started! Type: {battleType}, Participants: {participants.Count}");
        }

        private void InitializeBattleGrid()
        {
            battleGrid = new CombatGridTile[gridWidth, gridHeight];
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    battleGrid[x, y] = new CombatGridTile
                    {
                        Position = new Vector2Int(x, y),
                        IsOccupied = false,
                        OccupantId = null,
                        IsWalkable = true,
                        MovementCost = 1
                    };
                }
            }
        }

        private void PlaceParticipantsOnGrid()
        {
            foreach (var participant in participants)
            {
                if (IsValidPosition(participant.Position))
                {
                    battleGrid[participant.Position.x, participant.Position.y].IsOccupied = true;
                    battleGrid[participant.Position.x, participant.Position.y].OccupantId = participant.Character.Id;
                }
            }
        }

        private Vector2Int GetRandomPosition()
        {
            List<Vector2Int> availablePositions = new List<Vector2Int>();
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (!battleGrid[x, y].IsOccupied)
                    {
                        availablePositions.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (availablePositions.Count > 0)
            {
                return availablePositions[UnityEngine.Random.Range(0, availablePositions.Count)];
            }

            return Vector2Int.zero;
        }

        #endregion

        #region Turn Management

        private void StartNewTurn()
        {
            currentTurn++;
            turnTimer = turnTimeLimit;
            
            // Reset action points for current player
            currentPlayer.ActionPoints = actionPointsPerTurn;
            currentPlayer.IsReady = false;

            // Calculate valid moves for current player
            CalculateValidMoves(currentPlayer.Position, currentPlayer.ActionPoints);
            
            OnTurnChanged?.Invoke(currentPlayer);
            OnTurnTimerChanged?.Invoke(turnTimer);
            
            Debug.Log($"Turn {currentTurn} started for {currentPlayer.Character.Name}");
        }

        private void UpdateTurnTimer()
        {
            turnTimer -= Time.deltaTime;
            OnTurnTimerChanged?.Invoke(turnTimer);

            if (turnTimer <= 0)
            {
                // Auto-end turn
                EndTurn();
            }
        }

        public void EndTurn()
        {
            if (!isInCombat || battleStatus != BattleStatus.InProgress)
                return;

            // Process any queued actions
            ProcessActionQueue();

            // Move to next player
            currentPlayerIndex = (currentPlayerIndex + 1) % participants.Count;
            currentPlayer = participants[currentPlayerIndex];

            // Check if all players have had their turn
            if (currentPlayerIndex == 0)
            {
                // Round complete, check for battle end
                CheckBattleEnd();
            }

            if (battleStatus == BattleStatus.InProgress)
            {
                StartNewTurn();
            }
        }

        private void CheckBattleEnd()
        {
            var aliveParticipants = participants.Where(p => p.Character.CurrentHP > 0).ToList();
            
            if (aliveParticipants.Count <= 1)
            {
                EndBattle(aliveParticipants.FirstOrDefault());
            }
        }

        private void EndBattle(CombatParticipant winner)
        {
            battleStatus = BattleStatus.Finished;
            isInCombat = false;
            
            // Calculate rewards and experience
            if (winner != null)
            {
                CalculateBattleRewards(winner);
            }

            OnBattleStatusChanged?.Invoke(battleStatus);
            Debug.Log($"Battle ended! Winner: {(winner?.Character.Name ?? "None")}");
        }

        private void CalculateBattleRewards(CombatParticipant winner)
        {
            // Calculate experience based on battle type and participants
            int baseExp = 100;
            int participantBonus = participants.Count * 25;
            int rankBonus = (int)(winner.Character.Rank * 50);
            
            int totalExp = baseExp + participantBonus + rankBonus;
            
            // Award experience
            winner.Character.GainExperience(totalExp);
            
            Debug.Log($"{winner.Character.Name} gained {totalExp} experience!");
        }

        #endregion

        #region Movement and Positioning

        public void CalculateValidMoves(Vector2Int startPos, int actionPoints)
        {
            validMovePositions.Clear();
            
            // Simple movement calculation - can move to adjacent tiles within action points
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2Int targetPos = new Vector2Int(x, y);
                    int distance = CalculateDistance(startPos, targetPos);
                    
                    if (distance <= actionPoints && IsValidPosition(targetPos) && !battleGrid[x, y].IsOccupied)
                    {
                        validMovePositions.Add(targetPos);
                    }
                }
            }

            OnValidMovesUpdated?.Invoke(validMovePositions);
        }

        public bool TryMovePlayer(Vector2Int newPosition)
        {
            if (!isInCombat || battleStatus != BattleStatus.InProgress)
                return false;

            if (!validMovePositions.Contains(newPosition))
                return false;

            int movementCost = CalculateDistance(playerPosition, newPosition);
            if (movementCost > currentPlayer.ActionPoints)
                return false;

            // Update grid
            battleGrid[playerPosition.x, playerPosition.y].IsOccupied = false;
            battleGrid[playerPosition.x, playerPosition.y].OccupantId = null;
            
            playerPosition = newPosition;
            currentPlayer.Position = newPosition;
            
            battleGrid[newPosition.x, newPosition.y].IsOccupied = true;
            battleGrid[newPosition.x, newPosition.y].OccupantId = currentPlayer.Character.Id;

            // Consume action points
            currentPlayer.ActionPoints -= movementCost;

            // Recalculate valid moves
            CalculateValidMoves(newPosition, currentPlayer.ActionPoints);

            Debug.Log($"Player moved to {newPosition}, AP remaining: {currentPlayer.ActionPoints}");
            return true;
        }

        private int CalculateDistance(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
        }

        private bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < gridWidth && 
                   position.y >= 0 && position.y < gridHeight;
        }

        #endregion

        #region Combat Actions

        public void QueueAction(CombatAction action)
        {
            if (!isInCombat || battleStatus != BattleStatus.InProgress)
                return;

            // Validate action
            if (!ValidateAction(action))
                return;

            actionQueue.Enqueue(action);
            
            // Process immediately if not already processing
            if (!isProcessingActions)
            {
                ProcessActionQueue();
            }
        }

        private bool ValidateAction(CombatAction action)
        {
            // Check if player has enough action points
            if (action.ActionPointCost > currentPlayer.ActionPoints)
            {
                Debug.LogWarning($"Not enough action points! Required: {action.ActionPointCost}, Available: {currentPlayer.ActionPoints}");
                return false;
            }

            // Check if action is valid for current turn
            if (action.ActionType == ActionType.Move && !validMovePositions.Contains(action.TargetPosition))
            {
                Debug.LogWarning("Invalid move position!");
                return false;
            }

            return true;
        }

        private async void ProcessActionQueue()
        {
            if (isProcessingActions || actionQueue.Count == 0)
                return;

            isProcessingActions = true;

            while (actionQueue.Count > 0)
            {
                var action = actionQueue.Dequeue();
                
                // Consume action points
                currentPlayer.ActionPoints -= action.ActionPointCost;

                // Execute action
                await ExecuteAction(action);

                // Notify action performed
                OnActionPerformed?.Invoke(action);

                // Small delay between actions
                await Task.Delay(100);
            }

            isProcessingActions = false;
        }

        private async Task ExecuteAction(CombatAction action)
        {
            switch (action.ActionType)
            {
                case ActionType.Move:
                    await ExecuteMoveAction(action);
                    break;
                case ActionType.Attack:
                    await ExecuteAttackAction(action);
                    break;
                case ActionType.Jutsu:
                    await ExecuteJutsuAction(action);
                    break;
                case ActionType.Heal:
                    await ExecuteHealAction(action);
                    break;
                case ActionType.Item:
                    await ExecuteItemAction(action);
                    break;
                case ActionType.Flee:
                    await ExecuteFleeAction(action);
                    break;
            }
        }

        private async Task ExecuteMoveAction(CombatAction action)
        {
            if (TryMovePlayer(action.TargetPosition))
            {
                action.IsSuccessful = true;
                action.ResultMessage = $"Moved to {action.TargetPosition}";
            }
            else
            {
                action.IsSuccessful = false;
                action.ResultMessage = "Move failed";
            }
        }

        private async Task ExecuteAttackAction(CombatAction action)
        {
            var target = participants.FirstOrDefault(p => p.Character.Id == action.TargetId);
            if (target == null)
            {
                action.IsSuccessful = false;
                action.ResultMessage = "Target not found";
                return;
            }

            // Calculate damage
            int damage = CalculateDamage(currentPlayer.Character, target.Character, action);
            
            // Apply damage
            target.Character.TakeDamage(damage);
            
            action.DamageDealt = damage;
            action.IsSuccessful = true;
            action.ResultMessage = $"Dealt {damage} damage to {target.Character.Name}";

            // Check if target is defeated
            if (target.Character.CurrentHP <= 0)
            {
                action.ResultMessage += $" - {target.Character.Name} defeated!";
            }
        }

        private async Task ExecuteJutsuAction(CombatAction action)
        {
            // Jutsu execution logic would go here
            // For now, just consume action points
            action.IsSuccessful = true;
            action.ResultMessage = "Jutsu executed";
        }

        private async Task ExecuteHealAction(CombatAction action)
        {
            var target = participants.FirstOrDefault(p => p.Character.Id == action.TargetId);
            if (target == null)
            {
                action.IsSuccessful = false;
                action.ResultMessage = "Target not found";
                return;
            }

            // Calculate healing
            int healing = CalculateHealing(currentPlayer.Character, target.Character, action);
            
            // Apply healing
            target.Character.Heal(healing);
            
            action.HealingDone = healing;
            action.IsSuccessful = true;
            action.ResultMessage = $"Healed {target.Character.Name} for {healing} HP";
        }

        private async Task ExecuteItemAction(CombatAction action)
        {
            // Item usage logic would go here
            action.IsSuccessful = true;
            action.ResultMessage = "Item used";
        }

        private async Task ExecuteFleeAction(CombatAction action)
        {
            // Flee logic would go here
            action.IsSuccessful = true;
            action.ResultMessage = "Fled from battle";
            
            // End battle for this player
            EndBattle(null);
        }

        #endregion

        #region Combat Calculations

        private int CalculateDamage(Character attacker, Character defender, CombatAction action)
        {
            int baseAttack = 0;
            
            // Determine attack stat based on action type
            switch (action.ActionType)
            {
                case ActionType.Attack:
                    baseAttack = attacker.Strength;
                    break;
                case ActionType.Jutsu:
                    baseAttack = attacker.Ninjutsu;
                    break;
                case ActionType.Weapon:
                    baseAttack = attacker.Bukijutsu;
                    break;
            }

            // Calculate defense
            int defense = defender.CalculateDefense();
            
            // Apply elemental modifiers
            float elementalMultiplier = CalculateElementalMultiplier(attacker.PrimaryElement, defender.PrimaryElement);
            
            // Calculate final damage
            int damage = Mathf.RoundToInt((baseAttack - defense) * elementalMultiplier);
            
            // Ensure minimum damage
            return Mathf.Max(1, damage);
        }

        private int CalculateHealing(Character healer, Character target, CombatAction action)
        {
            int baseHealing = healer.MedNinRank switch
            {
                MedNinRank.Novice => 50,
                MedNinRank.FieldMedic => 100,
                MedNinRank.MasterMedic => 200,
                MedNinRank.LegendaryHealer => 400,
                _ => 25
            };

            // Apply intelligence bonus
            float intBonus = healer.Intelligence / 1000f;
            int finalHealing = Mathf.RoundToInt(baseHealing * (1 + intBonus));

            // Don't heal beyond max HP
            int maxHealing = target.MaxHP - target.CurrentHP;
            return Mathf.Min(finalHealing, maxHealing);
        }

        private float CalculateElementalMultiplier(Element attackerElement, Element defenderElement)
        {
            // Rock-paper-scissors elemental system
            if (attackerElement == Element.Water && defenderElement == Element.Fire) return 1.5f;
            if (attackerElement == Element.Fire && defenderElement == Element.Earth) return 1.5f;
            if (attackerElement == Element.Earth && defenderElement == Element.Water) return 1.5f;
            
            if (attackerElement == Element.Fire && defenderElement == Element.Water) return 0.75f;
            if (attackerElement == Element.Earth && defenderElement == Element.Fire) return 0.75f;
            if (attackerElement == Element.Water && defenderElement == Element.Earth) return 0.75f;
            
            return 1.0f;
        }

        #endregion

        #region Public Interface

        public bool IsInCombat => isInCombat;
        public BattleStatus CurrentBattleStatus => battleStatus;
        public int CurrentTurn => currentTurn;
        public float TurnTimer => turnTimer;
        public int ActionPointsPerTurn => actionPointsPerTurn;
        public CombatParticipant CurrentPlayer => currentPlayer;
        public List<CombatParticipant> Participants => participants;
        public CombatGridTile[,] BattleGrid => battleGrid;
        public Vector2Int PlayerPosition => playerPosition;
        public List<Vector2Int> ValidMovePositions => validMovePositions;

        public void ReadyUp()
        {
            if (currentPlayer != null)
            {
                currentPlayer.IsReady = true;
                
                // Check if all players are ready
                if (participants.All(p => p.IsReady))
                {
                    EndTurn();
                }
            }
        }

        public void Surrender()
        {
            if (currentPlayer != null)
            {
                // Remove player from battle
                participants.Remove(currentPlayer);
                
                // Check if battle should end
                if (participants.Count <= 1)
                {
                    EndBattle(participants.FirstOrDefault());
                }
                else
                {
                    // Adjust player index
                    if (currentPlayerIndex >= participants.Count)
                    {
                        currentPlayerIndex = 0;
                    }
                    currentPlayer = participants[currentPlayerIndex];
                    StartNewTurn();
                }
            }
        }

        #endregion
    }

    #region Supporting Classes

    [System.Serializable]
    public class CombatParticipant
    {
        public Character Character;
        public Vector2Int Position;
        public int ActionPoints;
        public bool IsReady;
    }

    [System.Serializable]
    public class CombatGridTile
    {
        public Vector2Int Position;
        public bool IsOccupied;
        public string OccupantId;
        public bool IsWalkable;
        public int MovementCost;
    }

    [System.Serializable]
    public class CombatAction
    {
        public string Id = System.Guid.NewGuid().ToString();
        public ActionType ActionType;
        public int ActionPointCost;
        public Vector2Int TargetPosition;
        public string TargetId;
        public bool IsSuccessful;
        public string ResultMessage;
        public int DamageDealt;
        public int HealingDone;
        public System.DateTime Timestamp = System.DateTime.UtcNow;
    }

    #endregion
}
