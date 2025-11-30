using System.Text.Json.Serialization;

// ============================================
// DTOs
// ============================================

public class ApprovalsVm
{
    public List<RequestItem> DirectPending { get; set; } = new();
    public List<RequestItem> FinalPending { get; set; } = new();
    public List<RequestItem> ImplementPending { get; set; } = new();
}

public class RequestItem
{
    public int Id { get; set; }
    public string RequestNumber { get; set; }
    public string SystemName { get; set; }
    public string RequestDetails { get; set; }
    public string Status { get; set; }
    public string CurrentStage { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RequestedBy { get; set; }
    public string CreatedByUsername { get; set; }
}

public class RequestActionHistoryDto
{
    public int Id { get; set; }
    public string Action { get; set; }
    public string ActionByUser { get; set; }
    public DateTime ActionDate { get; set; }
    public string Comments { get; set; }
    public string StatusBefore { get; set; }
    public string StatusAfter { get; set; }
}

public class RequestStageDto
{
    public int RequestId { get; set; }
    public string RequestType { get; set; }
    public string CurrentStage { get; set; }
    public string CurrentStatus { get; set; }
    public string RequestNumber { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class ApproveRequestDto
{
    public int RequestId { get; set; }
    public string RequestType { get; set; }
    public string Comments { get; set; }
    public int ApproverId { get; set; }
    public string requestedBy { get; set; }
}
