namespace PermissionSystemWeb.Models.ViewModels
{
    public class PendingApprovalVm
    {
        public string System { get; set; }
        public int Id { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Details { get; set; }
        public string Role { get; set; }
    }

}
