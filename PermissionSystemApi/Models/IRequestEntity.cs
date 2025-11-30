using System.ComponentModel.DataAnnotations.Schema;

namespace PermissionSystemApi.Models
{
    public interface IRequestEntity
    {
        int Id { get; set; }
        string? RequestNumber { get; set; }
        string? Status { get; set; }
        string? CurrentStage { get; set; }
        string? RejectionReason { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
        string? RequestDetails { get; set; }
        
        string? ManagerUsername { get; set; }
        [NotMapped]
        string? RequestedBy { get; set; }
        [NotMapped]
        string SystemName { get; set; }

        [NotMapped]
        string email { get; set; }
        [NotMapped]
        string empNumber { get; set; }


        string? CreatedByUsername {  get; set; }
        //  ICollection<RequestActionHistory> ActionHistory { get; set; }
    }
}
