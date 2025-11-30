namespace PermissionSystemWebweb.Models.ViewModels
{
    public class ActivityLogVm
    {
        public string User { get; set; }
        public string Action { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
