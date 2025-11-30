using Microsoft.EntityFrameworkCore;
using PermissionSystemApi.Data;
using PermissionSystemApi.Dtos;
using PermissionSystemApi.Models;

namespace PermissionSystemApi.Services
{
    public class RequestHistoryService
    {
        private readonly ApplicationDbContext _context;

        public RequestHistoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==============================================
        // 🔵 ADD ACTION TO HISTORY
        // ==============================================
        public async Task AddActionHistory(int requestId, string requestType, int actionBy,
   string action, string statusBefore, string statusAfter, string comments, string requestedBy)
        {
            var history = new RequestActionHistory();

            switch (requestType.ToLower())
            {
                case "ocity":
                    var ocityExists = await _context.OcityRequests.AnyAsync(o => o.Id == requestId);
                    if (!ocityExists)
                    {
                        throw new ArgumentException($"OcityRequest with ID {requestId} does not exist");
                    }
                    history.OcityRequestId = requestId;
                    break;

                case "invoiceq":
                    var invoiceExists = await _context.InvoiceQRequests.AnyAsync(i => i.Id == requestId);
                    if (!invoiceExists)
                    {
                        throw new ArgumentException($"InvoiceQRequest with ID {requestId} does not exist");
                    }
                    history.InvoiceQRequestId = requestId;
                    break;

                // إحذف case الـ periodicreview كلياً
                // case "periodicreview":
                //     var periodicExists = await _context.PeriodicReviews.AnyAsync(p => p.Id == requestId);
                //     if (!periodicExists)
                //     {
                //         throw new ArgumentException($"PeriodicReview with ID {requestId} does not exist");
                //     }
                //     break;

                default:
                    throw new ArgumentException($"Invalid request type: {requestType}");
            }

            history.ActionBy = actionBy;
            history.Action = action;
            history.StatusBefore = statusBefore;
            history.StatusAfter = statusAfter;
            history.Comments = comments;
            history.requestedBy = requestedBy;
            history.ActionDate = DateTime.Now;

            _context.RequestActionHistory.Add(history);
            await _context.SaveChangesAsync();
        }
        // ==============================================
        // 🔵 GET REQUEST HISTORY
        // ==============================================
        public async Task<List<RequestActionHistoryDto>> GetRequestHistory(int requestId, string requestType)
        {
            var history = await _context.RequestActionHistory
                .Where(h => h.RequestId == requestId && h.RequestType == requestType)
                .Include(h => h.ActionByUser)
                .OrderByDescending(h => h.ActionDate)
                .Select(h => new RequestActionHistoryDto
                {
                    Id = h.Id,
                    Action = h.Action,
                    ActionByUser = h.ActionByUser.FullName ?? h.ActionByUser.EmployeeId,
                    ActionDate = h.ActionDate,
                    Comments = h.Comments,
                    StatusBefore = h.StatusBefore,
                    StatusAfter = h.StatusAfter
                })
                .ToListAsync();

            return history;
        }

        // ==============================================
        // 🔵 GET ALL ACTIONS BY USER
        // ==============================================
        public async Task<List<RequestActionHistoryDto>> GetUserActions(int userId)
        {
            var actions = await _context.RequestActionHistory
                .Where(h => h.ActionBy == userId)
                .Include(h => h.ActionByUser)
                .OrderByDescending(h => h.ActionDate)
                .Select(h => new RequestActionHistoryDto
                {
                    Id = h.Id,
                    Action = h.Action,
                    ActionByUser = h.ActionByUser.FullName ?? h.ActionByUser.EmployeeId,
                    ActionDate = h.ActionDate,
                    Comments = h.Comments,
                    StatusBefore = h.StatusBefore,
                    StatusAfter = h.StatusAfter,
                    RequestId = h.RequestId,
                    RequestType = h.RequestType
                })
                .ToListAsync();

            return actions;
        }
    }
}