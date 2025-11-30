namespace PermissionSystemApi.Dtos
{
    // ============================================
    // DTOs
    // ============================================
    public class PendingApprovalVm
    {
        public string system { get; set; }
        public int id { get; set; }
        public string requestNumber { get; set; }
        public string status { get; set; }
        public string currentStage { get; set; }
        public DateTime createdAt { get; set; }
        public string details { get; set; }
        public string role { get; set; }
    }

}
