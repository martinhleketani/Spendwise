
using System;
using System.ComponentModel.DataAnnotations;

namespace SpendWise.API.Models
{
    public class Merchant
    {
        [Key]
        public int Id { get; set; }

        // Merchant or store name.
        [Required]
        [MaxLength(150)]
        public string BusinessName { get; set; } = string.Empty;

        // Person responsible for the merchant account.
        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        // Merchant contact email.
        [MaxLength(150)]
        public string? Email { get; set; }

        // Merchant contact number.
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        // Physical address of the store.
        [MaxLength(300)]
        public string? BusinessAddress { get; set; }

        // Pending, Active, Suspended or Closed.
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        // Merchant registration date.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Last account update.
        public DateTime? UpdatedAt { get; set; }
    }
}
