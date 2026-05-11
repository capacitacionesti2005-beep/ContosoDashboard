using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models
{
    [Table("Document")]
    public class Document
    {
        [Key]
        public int DocumentId { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; }

        [StringLength(2000)]
        public string Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } // ProjectDocs, TeamResources, PersonalFiles, Reports, Presentations, Other

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } // GUID-based path: AppData/uploads/{DocumentId}/{GUID}

        [StringLength(255)]
        public string FileName { get; set; } // Original filename for download

        [Required]
        public long FileSizeBytes { get; set; }

        [StringLength(100)]
        public string MimeType { get; set; } // e.g., application/pdf

        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int UploadedBy { get; set; } // FK to User

        [ForeignKey(nameof(UploadedBy))]
        public virtual User Uploader { get; set; }

        public int? AssociatedProjectId { get; set; } // Optional FK to Project

        [ForeignKey(nameof(AssociatedProjectId))]
        public virtual Project AssociatedProject { get; set; }

        [Required]
        public bool IsArchived { get; set; } = false;

        public DateTime? ArchivedAt { get; set; }

        [StringLength(50)]
        public string AntivirusStatus { get; set; } = "Pending"; // Pending, Scanning, Clean, Infected, Failed

        public DateTime? AntivirusCheckedAt { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string Tags { get; set; } // JSON array: ["tag1", "tag2"]

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

        public int? ModifiedBy { get; set; } // FK to User

        [ForeignKey(nameof(ModifiedBy))]
        public virtual User ModifiedByUser { get; set; }

        // Navigation properties
        public virtual List<DocumentShare> SharedWith { get; set; } = new();
        public virtual List<DocumentAuditLog> AuditLogs { get; set; } = new();

        [NotMapped]
        public string UploaderName => Uploader?.DisplayName ?? "Unknown";

        [NotMapped]
        public string? ProjectName => AssociatedProject?.Name;

        [NotMapped]
        public bool CanDelete { get; set; }

        [NotMapped]
        public bool CanShare { get; set; }
    }
}
