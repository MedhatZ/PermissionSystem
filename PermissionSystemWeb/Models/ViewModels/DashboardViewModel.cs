
// ============================================
// DTOs - تأكد إن دي متوافقة مع الـ Backend الجديد
// ============================================

public class DashboardViewModel
{
    public DashboardStatsVm Stats { get; set; }
    public List<RecentRequestVm> Recent { get; set; }
    public List<PendingApprovalVm> Pending { get; set; }
    public List<ActivityLogVm> Activity { get; set; }
    public DashboardChartVm Chart { get; set; }
}

public class DashboardStatsVm
{
    public int TotalInvoiceQ { get; set; }
    public int TotalOcity { get; set; }
    public int TotalPeriodicReviews { get; set; }
    public int TotalPermissions { get; set; }
    public int PendingFirst { get; set; }
    public int PendingFinal { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int ApprovedToday { get; set; }
}

 

public class PendingApprovalVm
{
    public int Id { get; set; }
    public string RequestNumber { get; set; }
    public string System { get; set; }
    public string Status { get; set; }
    public string CurrentStage { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Details { get; set; }
    public string Role { get; set; }
    
}

public class ActivityLogVm
{
    public string User { get; set; }
    public string Action { get; set; }
    public string RequestType { get; set; } // ✅ الجديد
    public int RequestId { get; set; } // ✅ الجديد
    public string StatusBefore { get; set; } // ✅ الجديد
    public string StatusAfter { get; set; } // ✅ الجديد
    public string Comments { get; set; } // ✅ الجديد
    public DateTime ActionDate { get; set; } // ✅ غيرنا من CreatedAt ل ActionDate
}

public class DashboardChartVm
{
    public List<ChartItem> InvoiceCount { get; set; } = new();
    public List<ChartItem> OcityCount { get; set; } = new();
    public List<ChartItem> PeriodicCount { get; set; } = new();
    public List<ChartItem> PermissionsCount { get; set; } = new();
}

public class ChartItem
{
    public string Status { get; set; }
    public int Count { get; set; }
}