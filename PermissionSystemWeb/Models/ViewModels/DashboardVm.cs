using PermissionSystemWeb.Models.DTO;

public class DashboardVm
{
    public int PendingFirst { get; set; }
    public int PendingSecond { get; set; }

    public int ApprovedToday { get; set; }
    public int RejectedToday { get; set; }

    public int InvoiceQTotal { get; set; }
    public int OcityTotal { get; set; }

    public List<RequestItem> AllInvoiceQ { get; set; }
    public List<RequestItem> AllOcity { get; set; }

    public List<DailyCount> Last14Days { get; set; }
    public List<DailyStatusCount> Last7Days { get; set; }

}
