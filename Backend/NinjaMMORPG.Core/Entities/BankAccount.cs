using System.ComponentModel.DataAnnotations;

namespace NinjaMMORPG.Core.Entities;

public class BankAccount
{
    public int Id { get; set; }
    public int CharacterId { get; set; }
    
    // Secure bank balance (cannot be stolen)
    public int Balance { get; set; } = 0;
    
    // Interest and bonuses
    public decimal InterestRate { get; set; } = 0.02m; // 2% base interest
    public DateTime LastInterestCalculation { get; set; } = DateTime.UtcNow;
    
    // Transaction history
    public virtual List<BankTransaction> Transactions { get; set; } = new();
    
    // Navigation Properties
    public virtual Character Character { get; set; } = null!;
    
    public void Deposit(int amount)
    {
        if (amount > 0)
        {
            Balance += amount;
            AddTransaction(TransactionType.Deposit, amount, "Deposit from pocket");
        }
    }
    
    public bool Withdraw(int amount)
    {
        if (amount > 0 && Balance >= amount)
        {
            Balance -= amount;
            AddTransaction(TransactionType.Withdrawal, amount, "Withdrawal to pocket");
            return true;
        }
        return false;
    }
    
    public bool Transfer(int amount, BankAccount recipient)
    {
        if (amount > 0 && Balance >= amount)
        {
            Balance -= amount;
            recipient.Balance += amount;
            
            AddTransaction(TransactionType.Transfer, amount, $"Transfer to {recipient.Character.Name}");
            recipient.AddTransaction(TransactionType.Transfer, amount, $"Transfer from {Character.Name}");
            
            return true;
        }
        return false;
    }
    
    private void AddTransaction(TransactionType type, int amount, string description)
    {
        var transaction = new BankTransaction
        {
            BankAccountId = Id,
            Type = type,
            Amount = amount,
            Description = description,
            Timestamp = DateTime.UtcNow
        };
        
        Transactions.Add(transaction);
    }
    
    public void CalculateInterest()
    {
        var now = DateTime.UtcNow;
        var daysSinceLastCalculation = (now - LastInterestCalculation).Days;
        
        if (daysSinceLastCalculation >= 1)
        {
            var dailyInterest = Balance * InterestRate / 365;
            var totalInterest = (int)(dailyInterest * daysSinceLastCalculation);
            
            if (totalInterest > 0)
            {
                Balance += totalInterest;
                AddTransaction(TransactionType.Interest, totalInterest, "Daily interest payment");
            }
            
            LastInterestCalculation = now;
        }
    }
}

public class BankTransaction
{
    public int Id { get; set; }
    public int BankAccountId { get; set; }
    public TransactionType Type { get; set; }
    public int Amount { get; set; }
    
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; }
    
    // Navigation Properties
    public virtual BankAccount BankAccount { get; set; } = null!;
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
