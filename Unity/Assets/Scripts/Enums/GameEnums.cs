namespace NinjaMMORPG.Enums
{
    public enum Village
    {
        None = 0,           // Outlaw status
        HiddenLeaf = 1,     // Forest/nature theme
        Stone = 2,          // Mountain/earth theme
        Mist = 3,           // Water/coastal theme
        Sand = 4,           // Desert theme
        Cloud = 5           // Sky/lightning theme
    }
    
    public enum Gender
    {
        Male = 1,
        Female = 2,
        NonBinary = 3
    }
    
    public enum Rank
    {
        Student = 1,        // Starting rank
        Genin = 2,          // Academy graduate
        Chunin = 3,         // Mid-level ninja
        Jounin = 4,         // High-level ninja
        SpecialJounin = 5,  // Elite ninja
        Kage = 6,           // Village leader (appointment only)
        Elder = 7           // Village elder (appointment only)
    }
    
    public enum MedNinRank
    {
        NoviceMedic = 1,    // Starting medical rank
        FieldMedic = 2,     // Basic healing abilities
        MasterMedic = 3,    // Advanced healing
        LegendaryHealer = 4 // Maximum healing efficiency
    }
    
    public enum Element
    {
        None = 0,           // Neutral element
        Fire = 1,           // Beats Earth, weak to Water
        Water = 2,          // Beats Fire, weak to Earth
        Earth = 3,          // Beats Water, weak to Fire
        Lightning = 4,      // Future expansion
        Wind = 5,           // Future expansion
        Ice = 6             // Future expansion
    }
    
    public enum CombatType
    {
        Ninjutsu = 1,       // Elemental techniques, uses Intelligence
        Genjutsu = 2,       // Illusion techniques, uses Willpower
        Bukijutsu = 3,      // Weapon techniques, uses Strength
        Taijutsu = 4        // Hand-to-hand combat, uses Speed
    }
    
    public enum ActionType
    {
        Move = 1,           // Movement on grid
        Attack = 2,         // Basic attack
        Jutsu = 3,          // Ninjutsu/Genjutsu
        Weapon = 4,         // Bukijutsu techniques
        Heal = 5,           // Medical ninja healing
        Item = 6,           // Use consumable items
        Flee = 7            // Attempt to escape
    }
    
    public enum MissionRank
    {
        D = 1,              // Available to Students and above
        C = 2,              // Available to Genin and above
        B = 3,              // Available to Chunin and above
        A = 4,              // Available to Jounin and above
        S = 5               // Available to Special Jounin and above
    }
    
    public enum MissionType
    {
        Combat = 1,         // Defeat enemies
        Delivery = 2,       // Transport items
        Escort = 3,         // Protect NPCs
        Investigation = 4,  // Gather information
        Training = 5,       // Improve stats
        Special = 6         // Unique events
    }
    
    public enum EquipmentSlot
    {
        Head = 1,           // Headband, mask, helmet
        Body = 2,           // Chest armor, robes
        ArmsLeft = 3,       // Left arm guards, gauntlets
        ArmsRight = 4,      // Right arm guards, gauntlets
        Legs = 5,           // Leg armor, protective pants
        Feet = 6            // Boots, sandals, speed gear
    }
    
    public enum EquipmentType
    {
        Weapon = 1,         // Swords, kunai, shuriken
        Armor = 2,          // Protective gear
        Accessory = 3,      // Rings, necklaces, etc.
        Tool = 4            // Utility items
    }
    
    public enum BattleStatus
    {
        Preparing = 1,
        InProgress = 2,
        Paused = 3,
        Finished = 4
    }
    
    public enum BattleType
    {
        PvE = 1,            // Player vs Environment
        PvP = 2,            // Player vs Player
        Mission = 3,        // Mission-related battle
        Training = 4        // Training spar
    }
    
    public enum ApplicationStatus
    {
        Pending = 1,
        Accepted = 2,
        Rejected = 3
    }
    
    public enum TransactionType
    {
        Deposit = 1,
        Withdrawal = 2,
        Transfer = 3,
        Interest = 4,
        MissionReward = 5,
        PvPReward = 6,
        LotteryWinnings = 7
    }
}
