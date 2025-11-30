namespace PermissionSystemWeb.Models.DTO
{
    public class DailyCount
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class DailyStatusCount
    {
        public DateTime Date { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
    }

}
