 
public class PeriodicReviewPageVm
{
    public ReviewOverviewVm Overview { get; set; }
    public List<ReviewItemVm> Items { get; set; }
    public List<ReviewHistoryVm> History { get; set; }
}

public class ReviewOverviewVm
{
    public bool HasActiveCycle { get; set; }
    public int ActiveCycleId { get; set; }
    public string ActiveCycleStatus { get; set; }
    public DateTime? ActiveCycleCreatedAt { get; set; }
    public int TotalPermissions { get; set; }
    public int InScopeThisCycle { get; set; }
    public int ReviewedCount { get; set; }
    public int RemainingCount { get; set; }
}

public class ReviewItemVm
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string EmployeeId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string SystemName { get; set; }
    public string PermissionDetails { get; set; }
    public string CurrentStatus { get; set; }
    public string CurrentStage { get; set; }
    public string RequestNumber { get; set; }
    public string SuggestedAction { get; set; }
}

public class SaveDecisionVm
{
    public int Id { get; set; }
    public string SuggestedAction { get; set; } // "Keep" or "Revoke"
}

public class ReviewHistoryVm
{
    public int Id { get; set; }
    public string RequestNumber { get; set; }
    public string Status { get; set; }
    public string CurrentStage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string ReviewDetails { get; set; }
}

public class SaveDecisionResult
{
    public int Saved { get; set; }
}