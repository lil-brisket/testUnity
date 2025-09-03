using System;
using System.Collections.Generic;
using UnityEngine;

namespace NinjaMMORPG.Models
{
    [System.Serializable]
    public class Character
    {
        [Header("Basic Information")]
        public int Id;
        public string Name = "";
        public Gender Gender;
        public Village Village;
        
        [Header("Resource Stats")]
        public int HP = 100;
        public int MaxHP = 100;
        public int CP = 100;
        public int MaxCP = 100;
        public int SP = 100;
        public int MaxSP = 100;
        
        [Header("Core Stats (Max: 250,000)")]
        public int Strength = 1;
        public int Intelligence = 1;
        public int Speed = 1;
        public int Willpower = 1;
        
        [Header("Combat Stats (Max: 500,000)")]
        public int Ninjutsu = 1;
        public int Genjutsu = 1;
        public int Bukijutsu = 1;
        public int Taijutsu = 1;
        
        [Header("Progression")]
        public Rank Rank = Rank.Student;
        public MedNinRank MedNinRank = MedNinRank.NoviceMedic;
        public int Level = 1;
        public long Experience = 0;
        public long MedNinExperience = 0;
        
        [Header("Social and Economic")]
        public int? ClanId;
        public int? SenseiId;
        public int PocketMoney = 1000;
        public int Reputation = 0;
        public int Infamy = 0;
        
        [Header("Location and Status")]
        public int CurrentLocationId = 1;
        public bool IsInBattle = false;
        public bool IsInHospital = false;
        public DateTime? HospitalEntryTime;
        public DateTime LastActivity = DateTime.UtcNow;
        
        [Header("Computed Properties")]
        public int DefenseValue => CalculateDefenseValue();
        public int CurrentAP = 100;
        public bool IsOutlaw => Village == Village.None;
        
        [Header("Equipment")]
        public List<Equipment> Equipment = new List<Equipment>();
        
        // Events
        public event Action<Character> OnStatsChanged;
        public event Action<Character> OnLocationChanged;
        public event Action<Character> OnBattleStateChanged;
        
        public Character()
        {
            // Initialize with default values
            LastActivity = DateTime.UtcNow;
        }
        
        public Character(CharacterData data) : this()
        {
            // Copy from data object
            Id = data.Id;
            Name = data.Name;
            Gender = data.Gender;
            Village = data.Village;
            HP = data.HP;
            MaxHP = data.MaxHP;
            CP = data.CP;
            MaxCP = data.MaxCP;
            SP = data.SP;
            MaxSP = data.MaxSP;
            Strength = data.Strength;
            Intelligence = data.Intelligence;
            Speed = data.Speed;
            Willpower = data.Willpower;
            Ninjutsu = data.Ninjutsu;
            Genjutsu = data.Genjutsu;
            Bukijutsu = data.Bukijutsu;
            Taijutsu = data.Taijutsu;
            Rank = data.Rank;
            MedNinRank = data.MedNinRank;
            Level = data.Level;
            Experience = data.Experience;
            MedNinExperience = data.MedNinExperience;
            ClanId = data.ClanId;
            SenseiId = data.SenseiId;
            PocketMoney = data.PocketMoney;
            Reputation = data.Reputation;
            Infamy = data.Infamy;
            CurrentLocationId = data.CurrentLocationId;
            IsInBattle = data.IsInBattle;
            IsInHospital = data.IsInHospital;
            HospitalEntryTime = data.HospitalEntryTime;
            LastActivity = data.LastActivity;
        }
        
        private int CalculateDefenseValue()
        {
            // Base defense from equipment and stats
            int baseDefense = 0;
            
            foreach (var equipment in Equipment)
            {
                baseDefense += equipment.DefenseBonus;
            }
            
            // Add stat-based defense
            baseDefense += (Strength + Intelligence + Speed + Willpower) / 1000;
            
            return Mathf.Max(1, baseDefense);
        }
        
        public bool CanUseJutsu(int cpCost, int spCost)
        {
            return CP >= cpCost && SP >= spCost;
        }
        
        public void ConsumeResources(int cpCost, int spCost)
        {
            CP = Mathf.Max(0, CP - cpCost);
            SP = Mathf.Max(0, SP - spCost);
            OnStatsChanged?.Invoke(this);
        }
        
        public void TakeDamage(int damage)
        {
            HP = Mathf.Max(0, HP - damage);
            
            if (HP == 0)
            {
                IsInHospital = true;
                HospitalEntryTime = DateTime.UtcNow;
            }
            
            OnStatsChanged?.Invoke(this);
        }
        
        public void Heal(int amount)
        {
            HP = Mathf.Min(MaxHP, HP + amount);
            
            if (HP > 0 && IsInHospital)
            {
                IsInHospital = false;
                HospitalEntryTime = null;
            }
            
            OnStatsChanged?.Invoke(this);
        }
        
        public void RestoreResources(int cpAmount, int spAmount)
        {
            CP = Mathf.Min(MaxCP, CP + cpAmount);
            SP = Mathf.Min(MaxSP, SP + spAmount);
            OnStatsChanged?.Invoke(this);
        }
        
        public void SetBattleState(bool inBattle)
        {
            IsInBattle = inBattle;
            OnBattleStateChanged?.Invoke(this);
        }
        
        public void SetLocation(int locationId)
        {
            CurrentLocationId = locationId;
            OnLocationChanged?.Invoke(this);
        }
        
        public void AddExperience(long amount)
        {
            Experience += amount;
            CheckLevelUp();
            OnStatsChanged?.Invoke(this);
        }
        
        public void AddMedNinExperience(long amount)
        {
            MedNinExperience += amount;
            CheckMedNinRankUp();
            OnStatsChanged?.Invoke(this);
        }
        
        private void CheckLevelUp()
        {
            // Simple level up calculation
            int newLevel = 1 + (int)(Experience / 1000);
            
            if (newLevel > Level)
            {
                Level = newLevel;
                
                // Increase max stats
                MaxHP += 10;
                MaxCP += 5;
                MaxSP += 5;
                
                // Restore to full
                HP = MaxHP;
                CP = MaxCP;
                SP = MaxSP;
                
                Debug.Log($"Character {Name} leveled up to {Level}!");
            }
        }
        
        private void CheckMedNinRankUp()
        {
            // Simple medical ninja rank up calculation
            MedNinRank newRank = MedNinRank.NoviceMedic;
            
            if (MedNinExperience >= 10000)
                newRank = MedNinRank.LegendaryHealer;
            else if (MedNinExperience >= 5000)
                newRank = MedNinRank.MasterMedic;
            else if (MedNinExperience >= 1000)
                newRank = MedNinRank.FieldMedic;
                
            if (newRank > MedNinRank)
            {
                MedNinRank = newRank;
                Debug.Log($"Character {Name} medical ninja rank increased to {MedNinRank}!");
            }
        }
        
        public bool CanAdvanceRank()
        {
            return Rank switch
            {
                Rank.Student => CanAdvanceToGenin(),
                Rank.Genin => CanAdvanceToChunin(),
                Rank.Chunin => CanAdvanceToJounin(),
                Rank.Jounin => CanAdvanceToSpecialJounin(),
                _ => false
            };
        }
        
        private bool CanAdvanceToGenin()
        {
            // Student → Genin: Level 5 and some basic requirements
            return Level >= 5;
        }
        
        private bool CanAdvanceToChunin()
        {
            // Genin → Chunin: Level 10 and some basic requirements
            return Level >= 10;
        }
        
        private bool CanAdvanceToJounin()
        {
            // Chunin → Jounin: Level 20 and some basic requirements
            return Level >= 20;
        }
        
        private bool CanAdvanceToSpecialJounin()
        {
            // Jounin → Special Jounin: Level 30 and maxed offensive stat
            return Level >= 30 && 
                   Mathf.Max(Ninjutsu, Mathf.Max(Genjutsu, Mathf.Max(Bukijutsu, Taijutsu))) >= 400000;
        }
        
        public void AdvanceRank()
        {
            if (!CanAdvanceRank()) return;
            
            Rank oldRank = Rank;
            Rank newRank = GetNextRank(Rank);
            
            Rank = newRank;
            Debug.Log($"Character {Name} advanced from {oldRank} to {newRank}!");
            
            OnStatsChanged?.Invoke(this);
        }
        
        private Rank GetNextRank(Rank currentRank)
        {
            return currentRank switch
            {
                Rank.Student => Rank.Genin,
                Rank.Genin => Rank.Chunin,
                Rank.Chunin => Rank.Jounin,
                Rank.Jounin => Rank.SpecialJounin,
                _ => currentRank
            };
        }
        
        public void EquipItem(Equipment equipment)
        {
            // Remove any existing equipment in the same slot
            Equipment.RemoveAll(e => e.Slot == equipment.Slot);
            
            // Add new equipment
            Equipment.Add(equipment);
            
            // Apply stat bonuses
            ApplyEquipmentStats();
            
            OnStatsChanged?.Invoke(this);
        }
        
        public void UnequipItem(EquipmentSlot slot)
        {
            Equipment.RemoveAll(e => e.Slot == slot);
            ApplyEquipmentStats();
            OnStatsChanged?.Invoke(this);
        }
        
        private void ApplyEquipmentStats()
        {
            // Reset stats to base values
            // In a real implementation, you'd store base stats separately
            
            // Apply equipment bonuses
            foreach (var equipment in Equipment)
            {
                Strength += equipment.StrengthBonus;
                Intelligence += equipment.IntelligenceBonus;
                Speed += equipment.SpeedBonus;
                Willpower += equipment.WillpowerBonus;
                Ninjutsu += equipment.NinjutsuBonus;
                Genjutsu += equipment.GenjutsuBonus;
                Bukijutsu += equipment.BukijutsuBonus;
                Taijutsu += equipment.TaijutsuBonus;
                MaxHP += equipment.HPBonus;
                MaxCP += equipment.CPBonus;
                MaxSP += equipment.SPBonus;
            }
        }
        
        public void UpdateFromData(CharacterData data)
        {
            // Update character from server data
            HP = data.HP;
            MaxHP = data.MaxHP;
            CP = data.CP;
            MaxCP = data.MaxCP;
            SP = data.SP;
            MaxSP = data.SP;
            CurrentAP = data.CurrentAP;
            CurrentLocationId = data.CurrentLocationId;
            IsInBattle = data.IsInBattle;
            IsInHospital = data.IsInHospital;
            PocketMoney = data.PocketMoney;
            Experience = data.Experience;
            MedNinExperience = data.MedNinExperience;
            
            OnStatsChanged?.Invoke(this);
        }
        
        public CharacterData ToData()
        {
            return new CharacterData
            {
                Id = Id,
                Name = Name,
                Gender = Gender,
                Village = Village,
                HP = HP,
                MaxHP = MaxHP,
                CP = CP,
                MaxCP = MaxCP,
                SP = SP,
                MaxSP = MaxSP,
                Strength = Strength,
                Intelligence = Intelligence,
                Speed = Speed,
                Willpower = Willpower,
                Ninjutsu = Ninjutsu,
                Genjutsu = Genjutsu,
                Bukijutsu = Bukijutsu,
                Taijutsu = Taijutsu,
                Rank = Rank,
                MedNinRank = MedNinRank,
                Level = Level,
                Experience = Experience,
                MedNinExperience = MedNinExperience,
                ClanId = ClanId,
                SenseiId = SenseiId,
                PocketMoney = PocketMoney,
                Reputation = Reputation,
                Infamy = Infamy,
                CurrentLocationId = CurrentLocationId,
                IsInBattle = IsInBattle,
                IsInHospital = IsInHospital,
                HospitalEntryTime = HospitalEntryTime,
                LastActivity = LastActivity,
                CurrentAP = CurrentAP
            };
        }
    }
    
    // Data transfer object for server communication
    [System.Serializable]
    public class CharacterData
    {
        public int Id;
        public string Name = "";
        public Gender Gender;
        public Village Village;
        public int HP = 100;
        public int MaxHP = 100;
        public int CP = 100;
        public int MaxCP = 100;
        public int SP = 100;
        public int MaxSP = 100;
        public int Strength = 1;
        public int Intelligence = 1;
        public int Speed = 1;
        public int Willpower = 1;
        public int Ninjutsu = 1;
        public int Genjutsu = 1;
        public int Bukijutsu = 1;
        public int Taijutsu = 1;
        public Rank Rank = Rank.Student;
        public MedNinRank MedNinRank = MedNinRank.NoviceMedic;
        public int Level = 1;
        public long Experience = 0;
        public long MedNinExperience = 0;
        public int? ClanId;
        public int? SenseiId;
        public int PocketMoney = 1000;
        public int Reputation = 0;
        public int Infamy = 0;
        public int CurrentLocationId = 1;
        public bool IsInBattle = false;
        public bool IsInHospital = false;
        public DateTime? HospitalEntryTime;
        public DateTime LastActivity = DateTime.UtcNow;
        public int CurrentAP = 100;
    }
}
