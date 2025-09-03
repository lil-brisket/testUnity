using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;

namespace NinjaMMORPG.Managers
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Inventory Settings")]
        [SerializeField] private int maxInventorySlots = 50;
        [SerializeField] private int maxEquipmentSlots = 6;
        [SerializeField] private float itemUseCooldown = 1f;

        // Inventory data
        private List<InventoryItem> inventoryItems = new List<InventoryItem>();
        private Dictionary<EquipmentSlot, Equipment> equippedItems = new Dictionary<EquipmentSlot, Equipment>();
        private Dictionary<string, DateTime> lastItemUseTimes = new Dictionary<string, DateTime>();

        // Events
        public event Action<InventoryItem> OnItemAdded;
        public event Action<InventoryItem> OnItemRemoved;
        public event Action<InventoryItem> OnItemUsed;
        public event Action<Equipment, EquipmentSlot> OnItemEquipped;
        public event Action<Equipment, EquipmentSlot> OnItemUnequipped;
        public event Action OnInventoryChanged;
        public event Action OnEquipmentChanged;

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
            InitializeEquipmentSlots();
        }

        #region Initialization

        private void InitializeEquipmentSlots()
        {
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                equippedItems[slot] = null;
            }
        }

        #endregion

        #region Inventory Management

        public bool AddItem(InventoryItem item)
        {
            if (item == null)
            {
                Debug.LogWarning("Cannot add null item to inventory");
                return false;
            }

            // Check if inventory is full
            if (inventoryItems.Count >= maxInventorySlots)
            {
                UIManager.Instance.ShowNotification("Inventory is full!");
                return false;
            }

            // Check if item can stack with existing items
            var existingItem = inventoryItems.FirstOrDefault(i => i.ItemId == item.ItemId && i.CanStack);
            if (existingItem != null && existingItem.CanStack)
            {
                existingItem.Quantity += item.Quantity;
                OnInventoryChanged?.Invoke();
                return true;
            }

            // Add new item
            inventoryItems.Add(item);
            OnItemAdded?.Invoke(item);
            OnInventoryChanged?.Invoke();

            Debug.Log($"Added {item.Quantity}x {item.Name} to inventory");
            return true;
        }

        public bool RemoveItem(string itemId, int quantity = 1)
        {
            var item = inventoryItems.FirstOrDefault(i => i.ItemId == itemId);
            if (item == null)
            {
                Debug.LogWarning($"Item {itemId} not found in inventory");
                return false;
            }

            if (item.Quantity < quantity)
            {
                Debug.LogWarning($"Not enough {item.Name} to remove. Requested: {quantity}, Available: {item.Quantity}");
                return false;
            }

            item.Quantity -= quantity;

            if (item.Quantity <= 0)
            {
                inventoryItems.Remove(item);
                OnItemRemoved?.Invoke(item);
            }

            OnInventoryChanged?.Invoke();
            Debug.Log($"Removed {quantity}x {item.Name} from inventory");
            return true;
        }

        public bool HasItem(string itemId, int quantity = 1)
        {
            var item = inventoryItems.FirstOrDefault(i => i.ItemId == itemId);
            return item != null && item.Quantity >= quantity;
        }

        public int GetItemCount(string itemId)
        {
            var item = inventoryItems.FirstOrDefault(i => i.ItemId == itemId);
            return item?.Quantity ?? 0;
        }

        public List<InventoryItem> GetItemsByType(ItemType itemType)
        {
            return inventoryItems.Where(i => i.ItemType == itemType).ToList();
        }

        public List<InventoryItem> GetItemsByRarity(ItemRarity rarity)
        {
            return inventoryItems.Where(i => i.Rarity == rarity).ToList();
        }

        public List<InventoryItem> SearchItems(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return new List<InventoryItem>(inventoryItems);

            return inventoryItems.Where(i => 
                i.Name.ToLower().Contains(searchTerm.ToLower()) ||
                i.Description.ToLower().Contains(searchTerm.ToLower())
            ).ToList();
        }

        public void SortInventory(InventorySortType sortType, bool ascending = true)
        {
            switch (sortType)
            {
                case InventorySortType.Name:
                    inventoryItems = ascending 
                        ? inventoryItems.OrderBy(i => i.Name).ToList()
                        : inventoryItems.OrderByDescending(i => i.Name).ToList();
                    break;
                case InventorySortType.Type:
                    inventoryItems = ascending
                        ? inventoryItems.OrderBy(i => i.ItemType).ThenBy(i => i.Name).ToList()
                        : inventoryItems.OrderByDescending(i => i.ItemType).ThenByDescending(i => i.Name).ToList();
                    break;
                case InventorySortType.Rarity:
                    inventoryItems = ascending
                        ? inventoryItems.OrderBy(i => i.Rarity).ThenBy(i => i.Name).ToList()
                        : inventoryItems.OrderByDescending(i => i.Rarity).ThenByDescending(i => i.Name).ToList();
                    break;
                case InventorySortType.Value:
                    inventoryItems = ascending
                        ? inventoryItems.OrderBy(i => i.Value).ThenBy(i => i.Name).ToList()
                        : inventoryItems.OrderByDescending(i => i.Value).ThenByDescending(i => i.Name).ToList();
                    break;
                case InventorySortType.Quantity:
                    inventoryItems = ascending
                        ? inventoryItems.OrderBy(i => i.Quantity).ThenBy(i => i.Name).ToList()
                        : inventoryItems.OrderByDescending(i => i.Quantity).ThenByDescending(i => i.Name).ToList();
                    break;
            }

            OnInventoryChanged?.Invoke();
        }

        #endregion

        #region Equipment Management

        public bool EquipItem(string itemId, EquipmentSlot slot)
        {
            var item = inventoryItems.FirstOrDefault(i => i.ItemId == itemId);
            if (item == null)
            {
                UIManager.Instance.ShowNotification("Item not found in inventory");
                return false;
            }

            if (item.ItemType != ItemType.Equipment)
            {
                UIManager.Instance.ShowNotification("This item cannot be equipped");
                return false;
            }

            var equipment = item as Equipment;
            if (equipment == null)
            {
                UIManager.Instance.ShowNotification("Invalid equipment item");
                return false;
            }

            if (equipment.EquipmentSlot != slot)
            {
                UIManager.Instance.ShowNotification($"This item cannot be equipped in the {slot} slot");
                return false;
            }

            // Check if slot is already occupied
            if (equippedItems[slot] != null)
            {
                // Unequip current item first
                UnequipItem(slot);
            }

            // Equip the new item
            equippedItems[slot] = equipment;
            RemoveItem(itemId, 1);
            
            OnItemEquipped?.Invoke(equipment, slot);
            OnEquipmentChanged?.Invoke();

            // Apply equipment bonuses to character
            ApplyEquipmentBonuses(equipment, true);

            UIManager.Instance.ShowNotification($"{equipment.Name} equipped in {slot} slot");
            return true;
        }

        public bool UnequipItem(EquipmentSlot slot)
        {
            if (equippedItems[slot] == null)
            {
                UIManager.Instance.ShowNotification($"No item equipped in {slot} slot");
                return false;
            }

            var equipment = equippedItems[slot];
            
            // Remove equipment bonuses from character
            ApplyEquipmentBonuses(equipment, false);

            // Add item back to inventory
            var inventoryItem = new InventoryItem
            {
                ItemId = equipment.ItemId,
                Name = equipment.Name,
                Description = equipment.Description,
                ItemType = ItemType.Equipment,
                Rarity = equipment.Rarity,
                Value = equipment.Value,
                Quantity = 1,
                CanStack = false
            };

            AddItem(inventoryItem);
            equippedItems[slot] = null;

            OnItemUnequipped?.Invoke(equipment, slot);
            OnEquipmentChanged?.Invoke();

            UIManager.Instance.ShowNotification($"{equipment.Name} unequipped from {slot} slot");
            return true;
        }

        public Equipment GetEquippedItem(EquipmentSlot slot)
        {
            return equippedItems.TryGetValue(slot, out var equipment) ? equipment : null;
        }

        public Dictionary<EquipmentSlot, Equipment> GetAllEquippedItems()
        {
            return new Dictionary<EquipmentSlot, Equipment>(equippedItems);
        }

        public bool IsSlotOccupied(EquipmentSlot slot)
        {
            return equippedItems[slot] != null;
        }

        private void ApplyEquipmentBonuses(Equipment equipment, bool isEquipping)
        {
            var character = CharacterManager.Instance.GetCurrentCharacter();
            if (character == null) return;

            var multiplier = isEquipping ? 1 : -1;

            // Apply stat bonuses
            character.Strength += equipment.StrengthBonus * multiplier;
            character.Intelligence += equipment.IntelligenceBonus * multiplier;
            character.Speed += equipment.SpeedBonus * multiplier;
            character.Willpower += equipment.WillpowerBonus * multiplier;
            character.Ninjutsu += equipment.NinjutsuBonus * multiplier;
            character.Genjutsu += equipment.GenjutsuBonus * multiplier;
            character.Bukijutsu += equipment.BukijutsuBonus * multiplier;
            character.Taijutsu += equipment.TaijutsuBonus * multiplier;

            // Apply resource bonuses
            character.MaxHP += equipment.HPBonus * multiplier;
            character.MaxCP += equipment.CPBonus * multiplier;
            character.MaxSP += equipment.SPBonus * multiplier;

            // Ensure current values don't exceed new max values
            character.CurrentHP = Mathf.Min(character.CurrentHP, character.MaxHP);
            character.CurrentCP = Mathf.Min(character.CurrentCP, character.MaxCP);
            character.CurrentSP = Mathf.Min(character.CurrentSP, character.MaxSP);
        }

        #endregion

        #region Item Usage

        public async Task<bool> UseItem(string itemId, string targetId = null)
        {
            var item = inventoryItems.FirstOrDefault(i => i.ItemId == itemId);
            if (item == null)
            {
                UIManager.Instance.ShowNotification("Item not found");
                return false;
            }

            // Check cooldown
            if (lastItemUseTimes.TryGetValue(itemId, out var lastUseTime))
            {
                var timeSinceLastUse = DateTime.UtcNow - lastUseTime;
                if (timeSinceLastUse.TotalSeconds < itemUseCooldown)
                {
                    var remainingTime = itemUseCooldown - (float)timeSinceLastUse.TotalSeconds;
                    UIManager.Instance.ShowNotification($"Item on cooldown: {remainingTime:F1}s remaining");
                    return false;
                }
            }

            // Use the item based on its type
            bool success = false;
            switch (item.ItemType)
            {
                case ItemType.Consumable:
                    success = await UseConsumableItem(item, targetId);
                    break;
                case ItemType.Equipment:
                    success = await UseEquipmentItem(item, targetId);
                    break;
                case ItemType.Material:
                    success = await UseMaterialItem(item, targetId);
                    break;
                default:
                    UIManager.Instance.ShowNotification("This item cannot be used");
                    return false;
            }

            if (success)
            {
                // Update cooldown
                lastItemUseTimes[itemId] = DateTime.UtcNow;

                // Remove one item from stack
                RemoveItem(itemId, 1);

                OnItemUsed?.Invoke(item);
                UIManager.Instance.ShowNotification($"Used {item.Name}");
            }

            return success;
        }

        private async Task<bool> UseConsumableItem(InventoryItem item, string targetId)
        {
            var character = CharacterManager.Instance.GetCurrentCharacter();
            if (character == null) return false;

            // Simulate item use time
            await Task.Delay(500);

            // Apply consumable effects based on item properties
            // This would be more sophisticated in a real implementation
            if (item.Name.Contains("HP Potion"))
            {
                var healAmount = 50; // Base heal amount
                character.CurrentHP = Mathf.Min(character.CurrentHP + healAmount, character.MaxHP);
                UIManager.Instance.ShowNotification($"Restored {healAmount} HP");
                return true;
            }
            else if (item.Name.Contains("CP Potion"))
            {
                var restoreAmount = 50; // Base restore amount
                character.CurrentCP = Mathf.Min(character.CurrentCP + restoreAmount, character.MaxCP);
                UIManager.Instance.ShowNotification($"Restored {restoreAmount} CP");
                return true;
            }
            else if (item.Name.Contains("SP Potion"))
            {
                var restoreAmount = 50; // Base restore amount
                character.CurrentSP = Mathf.Min(character.CurrentSP + restoreAmount, character.MaxSP);
                UIManager.Instance.ShowNotification($"Restored {restoreAmount} SP");
                return true;
            }

            UIManager.Instance.ShowNotification("Unknown consumable item");
            return false;
        }

        private async Task<bool> UseEquipmentItem(InventoryItem item, string targetId)
        {
            // Equipment items are typically equipped rather than "used"
            // This could be for special equipment with active abilities
            UIManager.Instance.ShowNotification("Equipment items should be equipped, not used");
            return false;
        }

        private async Task<bool> UseMaterialItem(InventoryItem item, string targetId)
        {
            // Material items are typically used in crafting or quests
            UIManager.Instance.ShowNotification("Material items are used for crafting and quests");
            return false;
        }

        #endregion

        #region Inventory Queries

        public int GetInventoryCount()
        {
            return inventoryItems.Count;
        }

        public int GetTotalItemCount()
        {
            return inventoryItems.Sum(i => i.Quantity);
        }

        public int GetEmptySlots()
        {
            return maxInventorySlots - inventoryItems.Count;
        }

        public float GetInventoryWeight()
        {
            return inventoryItems.Sum(i => i.Weight * i.Quantity);
        }

        public int GetInventoryValue()
        {
            return inventoryItems.Sum(i => i.Value * i.Quantity);
        }

        public List<InventoryItem> GetItemsBySlot(EquipmentSlot slot)
        {
            return inventoryItems.Where(i => 
                i.ItemType == ItemType.Equipment && 
                (i as Equipment)?.EquipmentSlot == slot
            ).ToList();
        }

        public bool IsInventoryFull()
        {
            return inventoryItems.Count >= maxInventorySlots;
        }

        #endregion

        #region Utility Methods

        public void ClearInventory()
        {
            inventoryItems.Clear();
            OnInventoryChanged?.Invoke();
        }

        public void ClearEquipment()
        {
            foreach (var slot in equippedItems.Keys.ToList())
            {
                if (equippedItems[slot] != null)
                {
                    UnequipItem(slot);
                }
            }
        }

        public void ResetItemCooldowns()
        {
            lastItemUseTimes.Clear();
        }

        public float GetItemCooldownRemaining(string itemId)
        {
            if (!lastItemUseTimes.TryGetValue(itemId, out var lastUseTime))
                return 0f;

            var timeSinceLastUse = DateTime.UtcNow - lastUseTime;
            var remaining = itemUseCooldown - (float)timeSinceLastUse.TotalSeconds;
            
            return Mathf.Max(0f, remaining);
        }

        #endregion

        #region Public Interface

        public List<InventoryItem> InventoryItems => new List<InventoryItem>(inventoryItems);
        public Dictionary<EquipmentSlot, Equipment> EquippedItems => new Dictionary<EquipmentSlot, Equipment>(equippedItems);
        public int MaxInventorySlots => maxInventorySlots;
        public int MaxEquipmentSlots => maxEquipmentSlots;
        public float ItemUseCooldown => itemUseCooldown;

        public async Task<bool> LoadInventoryFromBackend()
        {
            try
            {
                // This would load inventory from the backend
                // For now, just return true
                await Task.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading inventory: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveInventoryToBackend()
        {
            try
            {
                // This would save inventory to the backend
                // For now, just return true
                await Task.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving inventory: {ex.Message}");
                return false;
            }
        }

        #endregion
    }

    #region Supporting Classes and Enums

    [System.Serializable]
    public class InventoryItem
    {
        public string ItemId;
        public string Name;
        public string Description;
        public ItemType ItemType;
        public ItemRarity Rarity;
        public int Value;
        public int Quantity;
        public float Weight;
        public bool CanStack;
        public Sprite Icon;
        public DateTime AcquiredAt = DateTime.UtcNow;
    }

    [System.Serializable]
    public class Equipment : InventoryItem
    {
        public EquipmentSlot EquipmentSlot;
        public EquipmentType EquipmentType;
        public Element ElementalAffinity;
        public int LevelRequirement;
        public int StrengthBonus;
        public int IntelligenceBonus;
        public int SpeedBonus;
        public int WillpowerBonus;
        public int NinjutsuBonus;
        public int GenjutsuBonus;
        public int BukijutsuBonus;
        public int TaijutsuBonus;
        public int HPBonus;
        public int CPBonus;
        public int SPBonus;
        public bool IsEnchanted;
        public List<string> Enchantments = new List<string>();
    }

    public enum ItemType
    {
        Consumable,
        Equipment,
        Material,
        Quest,
        Currency
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    public enum InventorySortType
    {
        Name,
        Type,
        Rarity,
        Value,
        Quantity,
        Weight
    }

    #endregion
}
