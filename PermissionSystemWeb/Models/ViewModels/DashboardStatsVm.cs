namespace PermissionSystemWeb.Models.ViewModels
{
    public class DashboardStatsVm
    {
        public int TotalInvoiceQ { get; set; }
        public int TotalOcity { get; set; }
        public int PendingFirst { get; set; }
        public int PendingFinal { get; set; }
        public int ApprovedToday { get; set; }
        public int RejectedToday { get; set; }
        public int Rejected { get; set; }

    }

}
