using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models
{
    [Table("DocumentAuditLog")]
    public class DocumentAuditLog
    {
        [Key]
        public long AuditId { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [ForeignKey(nameof(DocumentId))]
        public virtual Document Document { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } // Upload, Download, Share, Revoke, Edit, Delete, Archive

        [StringLength(500)]
        public string Details { get; set; } // JSON or text describing action

        [StringLength(50)]
        public string IpAddress { get; set; }

        [StringLength(500)]
        public string UserAgent { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
