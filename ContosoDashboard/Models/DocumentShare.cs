using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models
{
    [Table("DocumentShare")]
    public class DocumentShare
    {
        [Key]
        public int DocumentShareId { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [ForeignKey(nameof(DocumentId))]
        public virtual Document Document { get; set; }

        [Required]
        public int SharedBy { get; set; } // FK to User (original document owner)

        [ForeignKey(nameof(SharedBy))]
        public virtual User SharedByUser { get; set; }

        [Required]
        public int SharedWith { get; set; } // FK to User (recipient)

        [ForeignKey(nameof(SharedWith))]
        public virtual User SharedWithUser { get; set; }

        [Required]
        public DateTime SharedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RevokedAt { get; set; } // Null = active share, set = inactive

        [Required]
        public bool NotificationSent { get; set; } = false;

        public int? NotificationId { get; set; } // Optional FK to Notification
    }
}
