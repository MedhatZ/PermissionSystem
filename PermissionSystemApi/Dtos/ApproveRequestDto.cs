using System.ComponentModel.DataAnnotations;

namespace PermissionSystemApi.Dtos
{
    public class ApproveRequestDto
    {
        [Required]
        public int RequestId { get; set; }

        [Required]
        public string RequestType { get; set; }  // "InvoiceQ", "Ocity", etc.

        [Required]
        public int ApproverId { get; set; }

        public string? Comments { get; set; }
        public string requestedBy { get; set; }
    }

    public class RejectRequestDto
    {
        [Required]
        public int RequestId { get; set; }

        [Required]
        public string RequestType { get; set; } = string.Empty;

        [Required]
        public int ApproverId { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        public string? AdditionalComments { get; set; } // هذا اللي كان ناقص
        public string? requestedBy { get; set; }
    }

    public class SubmitRequestDto
    {
        [Required]
        public string RequestType { get; set; } = string.Empty;

        [Required]
        public string RequestDetails { get; set; } = string.Empty;

        public string CreatedByUsername { get; set; } = string.Empty;
        public string ManagerUsername { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public int CreatedByUserId { get; set; }
        public string? Comments { get; set; }
    }

    public class RequestActionHistoryDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string ActionByUser { get; set; } = string.Empty;
        public DateTime ActionDate { get; set; }
        public string? Comments { get; set; }
        public string? StatusBefore { get; set; }
        public string? StatusAfter { get; set; }
        public int RequestId { get; set; }
        public string RequestType { get; set; }
    }

    public class RequestStageDto
    {
        public int RequestId { get; set; }
        public string RequestType { get; set; } = string.Empty;
        public string CurrentStage { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public string RequestNumber { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }

    public class UpdateStageDto
    {
        public int RequestId { get; set; }
        public string RequestType { get; set; } = string.Empty;
        public string NewStage { get; set; } = string.Empty;
        public string? NewStatus { get; set; }
        public int UpdatedBy { get; set; }
        public string? Comments { get; set; }
        public string? requestedBy { get; set; }
    }
}