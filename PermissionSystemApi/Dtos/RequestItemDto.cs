namespace PermissionSystemApi.Dtos
{
    public class RequestItemDto
    {
        public int Id { get; set; }
        public string SystemName { get; set; }
        public string RequestDetails { get; set; }
        public string Status { get; set; }
        public string CurrentStage { get; set; }
        public DateTime CreatedAt { get; set; }
        public string RequestNumber { get; set; }
    }

    public class UserRequestVm
    {
        public int Id { get; set; }
        public string SystemName { get; set; }
        public string Status { get; set; }
        public string CurrentStage { get; set; }
        public string RequestNumber { get; set; }
        public string Details { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // New fields (needed by front-end)
        public string RequestedBy { get; set; } = "";

        public string? ManagerUsername { get; set; } = "";
        public string ApprovedBy { get; set; } = "";
        public string RequestType { get; set; } = "";
        public string PermissionLevel { get; set; } = "";

        public string TaskDetail { get; set; } = "";
        public DateTime? UserModificationDate { get; set; }
        public string UpdateType { get; set; } = "";
        public DateTime? UpdateDate { get; set; }
        public string RoleModified { get; set; } = "";
        public string CreatedByUsername { get; set; } = "";



        public List<string> Permissions { get; set; } = new();

        public List<HistoryItem> History { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
        public string empNumber { get; set; } = "";
        public string email { get; set; } = "";
    }

    public class HistoryItem
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
        public string UserName { get; set; }
        public string Notes { get; set; }
    }
    public class Comment
    {
        public string UserName { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}