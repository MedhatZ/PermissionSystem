namespace PermissionSystemApi.Dtos
{

    public class ReviewItemDto
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
}
