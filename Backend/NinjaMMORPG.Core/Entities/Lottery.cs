using System.ComponentModel.DataAnnotations;

namespace NinjaMMORPG.Core.Entities;

public class Lottery
{
    public int Id { get; set; }
    
    // Lottery details
    public DateTime DrawDate { get; set; }
    public int TicketPrice { get; set; } = 1000; // 1,000 Ryo per ticket
    public bool IsActive { get; set; } = true;
    public bool IsDrawn { get; set; } = false;
    
    // Prize pool
    public int TotalTicketsSold { get; set; } = 0;
    public int TotalPrizePool => TotalTicketsSold * TicketPrice;
    
    // Winner information
    public int? WinningTicketId { get; set; }
    public int? WinnerCharacterId { get; set; }
    public DateTime? DrawnAt { get; set; }
    
    // Navigation Properties
    public virtual List<LotteryTicket> Tickets { get; set; } = new();
    public virtual LotteryTicket? WinningTicket { get; set; }
    public virtual Character? Winner { get; set; }
    
    public void AddTicket(LotteryTicket ticket)
    {
        if (IsActive && !IsDrawn)
        {
            Tickets.Add(ticket);
            TotalTicketsSold++;
        }
    }
    
    public void DrawWinner()
    {
        if (IsDrawn || TotalTicketsSold == 0)
            return;
            
        // Random selection from all tickets
        var random = new Random();
        var winningTicketIndex = random.Next(TotalTicketsSold);
        var winningTicket = Tickets[winningTicketIndex];
        
        WinningTicketId = winningTicket.Id;
        WinnerCharacterId = winningTicket.CharacterId;
        DrawnAt = DateTime.UtcNow;
        IsDrawn = true;
        IsActive = false;
        
        // Award prize to winner
        if (winningTicket.Character?.BankAccount != null)
        {
            winningTicket.Character.BankAccount.Balance += TotalPrizePool;
        }
    }
    
    public bool CanPurchaseTicket()
    {
        return IsActive && !IsDrawn;
    }
}

public class LotteryTicket
{
    public int Id { get; set; }
    public int LotteryId { get; set; }
    public int CharacterId { get; set; }
    
    // Ticket details
    public int TicketNumber { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual Lottery Lottery { get; set; } = null!;
    public virtual Character Character { get; set; } = null!;
    
    public bool IsWinningTicket(int winningTicketId)
    {
        return Id == winningTicketId;
    }
}
