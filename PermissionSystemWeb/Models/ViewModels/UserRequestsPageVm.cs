using PermissionSystemWeb.Models.ViewModels;
using System.Collections.Generic;
using System.Xml.Linq;


// ============================================
// DTOs - تأكد إن دي متوافقة مع الـ Backend الجديد
// ============================================

public class UserRequestsPageVm
{
    public List<RequestItemDto> Active { get; set; } = new();
    public List<RequestItemDto> Closed { get; set; } = new();
}

public class RequestItemDto
{
    public int Id { get; set; }
    public string SystemName { get; set; }
    public string RequestDetails { get; set; }
    public string Status { get; set; }
    public string CurrentStage { get; set; }
    public string RequestNumber { get; set; }
    public DateTime CreatedAt { get; set; }
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
    public string RequestedBy { get; set; }
    public string ApprovedBy { get; set; }
    public string RequestType { get; set; }
    public string PermissionLevel { get; set; }
    public List<string> Permissions { get; set; }
    public List<HistoryItem> History { get; set; }
    public List<Comment> Comments { get; set; }

    public string? empNumber { get; set; }
    public string? email { get; set; }


    public string TaskDetail { get; set; }
    
    public DateTime? UserModificationDate { get; set; }
    public string UpdateType { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string RoleModified { get; set; }
    public string CreatedByUsername { get; set; }
    public string ManagerUsername { get; set; }


}

public class CreateRequestVm
{
    public string? SystemName { get; set; }
    public string? Name { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? InvoiceQJobTitle { get; set; }
    public string? OcityJobTitle { get; set; }

    public string? Unit { get; set; }
    public string? Project { get; set; }
    public string? RequestDetails { get; set; }
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

public class BulkUploadVm
{
    public IFormFile ExcelFile { get; set; }
    public string SystemType { get; set; }
}

public class BulkErrorVm
{
    public int RowNumber { get; set; }
    public string FieldName { get; set; }
    public string Message { get; set; }
    public string Value { get; set; }
}
public class BulkResultVm
{
    public int Success { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}

 
public class ApiSettings
{
    public string BaseUrl { get; set; }
}
