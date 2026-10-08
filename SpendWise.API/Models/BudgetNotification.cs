
using System;

namespace SpendWise.API.Models
{
    public class BudgetNotification
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Category { get; set; } = string.Empty;

        public int Threshold { get; set; }

        public string Message { get; set; } = string.Empty;

        public int Month { get; set; }

        public int Year { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
