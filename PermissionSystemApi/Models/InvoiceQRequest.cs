using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PermissionSystemApi.Models
{
    public class InvoiceQRequest : IRequestEntity
    {
        [Key]
        public int Id { get; set; }

        public string? RequestNumber { get; set; } = GenerateRequestNumber();

        public string? RequestDetails { get; set; }

        public string? Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public string? CurrentStage { get; set; } = "Submitted"; // Submitted, UnderReview, Approved, Rejected
        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string TaskDetail { get; set; }        
        public string RequestType { get; set; }

        public DateTime? UpdatedAt { get; set; }
        [NotMapped]
        public string SystemName { get; set; } = "InvoiceQ";
        
        [NotMapped]
        public string? RequestedBy { get; set; }

        
        public string? email { get; set; }
        
        public string? empNumber { get; set; }
        public string? CreatedByUsername { get; set; }

        public string? ManagerUsername { get; set; }

        public DateTime? UserModificationDate { get; set; }
        public string? UpdateType { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? RoleModified { get; set; }

        // Navigation property for action history
        //  public virtual ICollection<RequestActionHistory>? ActionHistory { get; set; }

        private static string GenerateRequestNumber()
        {
            return $"INV-{DateTime.Now:yyyyMMdd-HHmmss}";
        }
    }

    public class OcityRequest : IRequestEntity
    {
        [Key]
        public int Id { get; set; }

        public string? RequestNumber { get; set; } = GenerateRequestNumber();

        public string? RequestDetails { get; set; }

        public string TaskDetail { get; set; }        
        public string RequestType { get; set; }

        [NotMapped]
        public string? email { get; set; }
        [NotMapped]
        public string? empNumber { get; set; }

        public string? Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public string? CurrentStage { get; set; } = "Submitted"; // Submitted, UnderReview, Approved, Rejected
        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
        [NotMapped]
        public string SystemName { get; set; } = "Ocity";
        [NotMapped]
        public string? RequestedBy { get; set; }

        public string? CreatedByUsername { get; set; }

        public string? ManagerUsername { get; set; }


        public DateTime? UserModificationDate { get; set; }
        public string? UpdateType { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? RoleModified { get; set; }

        // Navigation property for action history
        // public virtual ICollection<RequestActionHistory>? ActionHistory { get; set; }

        private static string GenerateRequestNumber()
        {
            return $"OCITY-{DateTime.Now:yyyyMMdd-HHmmss}";
        }
    }

    public class PeriodicReview : IRequestEntity
    {
        [Key]
        public int Id { get; set; }

        public string? RequestNumber { get; set; } = GenerateRequestNumber();

        public string? ReviewDetails { get; set; }

        public string? Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public string? CurrentStage { get; set; } = "Submitted"; // Submitted, UnderReview, Approved, Rejected
        public string? RejectionReason { get; set; }
        [NotMapped]
        public string? SystemName { get; set; }

        [NotMapped]
        public string? email { get; set; }
        [NotMapped]
        public string? empNumber { get; set; }

        public string? CreatedByUsername { get; set; }
        public string? ManagerUsername { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [NotMapped]
        public string? RequestedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation property for action history
     //  public virtual ICollection<RequestActionHistory>? ActionHistory { get; set; }

        // Implement IRequestEntity interface
        public string? RequestDetails
        {
            get => ReviewDetails;
            set => ReviewDetails = value;
        }

        private static string GenerateRequestNumber()
        {
            return $"REVIEW-{DateTime.Now:yyyyMMdd-HHmmss}";
        }
    }

    public class PermissionRecord : IRequestEntity
    {
        [Key]
        public int Id { get; set; }

        public string? RequestNumber { get; set; } = GenerateRequestNumber();

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }
        [NotMapped]
        public string? RequestedBy { get; set; }
        public string? ManagerUsername { get; set; }
        public string? PermissionDetails { get; set; }
        public string? CreatedByUsername { get; set; }
        public string? Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        [NotMapped]
        public string? SystemName { get; set; }
        public string? CurrentStage { get; set; } = "Submitted"; // Submitted, UnderReview, Approved, Rejected
        public string? RejectionReason { get; set; }
        [NotMapped]
        public string? email { get; set; }
        [NotMapped]
        public string? empNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property for action history
      //  public virtual ICollection<RequestActionHistory>? ActionHistory { get; set; }

        // Implement IRequestEntity interface
        public string? RequestDetails
        {
            get => PermissionDetails;
            set => PermissionDetails = value;
        }

        private static string GenerateRequestNumber()
        {
            return $"PERM-{DateTime.Now:yyyyMMdd-HHmmss}";
        }
    }

    public class RequestActionHistory
    {
        [Key]
        public int Id { get; set; }

        public int RequestId { get; set; }

        [StringLength(50)]
        public string? RequestType { get; set; } // 'InvoiceQ', 'Ocity', 'PeriodicReview', 'Permission'

        [StringLength(100)]
        public string? Action { get; set; } // Submitted, Approved, Rejected, Returned, UnderReview

        public int ActionBy { get; set; }

        [ForeignKey("ActionBy")]
        public User? ActionByUser { get; set; }

        public DateTime ActionDate { get; set; } = DateTime.Now;

        public string? Comments { get; set; } // Reason for rejection or any notes

        [StringLength(50)]
        public string? StatusBefore { get; set; }

        [StringLength(50)]
        public string? StatusAfter { get; set; }

        public int OcityRequestId { get; set; } 
        public int InvoiceQRequestId { get; set; }

        //public int PeriodicReviewId { get; set; }
        public string? requestedBy { get; set; }


    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        public string? EmployeeId { get; set; }

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<ApprovalLog>? ApprovalLogs { get; set; }
        public virtual ICollection<PermissionRecord>? PermissionRecords { get; set; }
        public virtual ICollection<ManagerAssignment>? ManagerAssignments { get; set; }
        public virtual ICollection<RequestActionHistory>? ActionHistories { get; set; }
    }
    public class ApprovalLog
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public string? Action { get; set; }

        public string? Status { get; set; } // Pending, Approved, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public string? Comments { get; set; } // Added for rejection reasons
    }

    public class ManagerAssignment
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [StringLength(50)]
        public string? SystemName { get; set; } // InvoiceQ or Ocity

        [StringLength(50)]
        public string? RoleType { get; set; } // DirectManager, FinancialManager, ProjectManager

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

