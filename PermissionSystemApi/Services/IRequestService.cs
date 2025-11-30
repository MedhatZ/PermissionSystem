using PermissionSystemApi.Dtos;

namespace PermissionSystemApi.Services
{
    public interface IRequestService
    {
        Task<List<RequestItemDto>> GetAllInvoiceQAsync();
        Task<List<RequestItemDto>> GetAllOcityAsync();
        Task<List<RequestItemDto>> GetAllForUser(string employeeId);
        Task<UserRequestVm?> GetRequestDetails(int id, string system);
    }
}