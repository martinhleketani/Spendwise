using System;
using System.ComponentModel.DataAnnotations;

namespace SpendWise.API.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        // Person or system responsible for the activity.
        [Required]
        [MaxLength(20)]
        public string ActorType { get; set; } = "System";

        public int? ActorId { get; set; }

        [MaxLength(150)]
        public string ActorName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string ActorEmail { get; set; } = string.Empty;

        // Retained for compatibility with historical administrator logs.
        public int? AdminUserId { get; set; }

        [MaxLength(150)]
        public string AdminName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string AdminEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Module { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EntityType { get; set; }

        [MaxLength(100)]
        public string? EntityId { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}