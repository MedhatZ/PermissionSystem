using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PermissionSystemApi.Data;
using PermissionSystemApi.Dtos;
using PermissionSystemApi.Models;

namespace PermissionSystemApi.Services
{
    public class ApprovalService
    {
        private readonly ApplicationDbContext _context;
        private readonly RequestHistoryService _historyService;

        public ApprovalService(ApplicationDbContext context, RequestHistoryService historyService)
        {
            _context = context;
            _historyService = historyService;
        }

        // ==============================================
        // 🔵 APPROVE FIRST LEVEL (Direct Manager)
        // ==============================================
        public async Task<bool> ApproveFirstLevel(ApproveRequestDto dto)
        {
            var request = await GetRequest(dto.RequestId, dto.RequestType);
            if (request == null || request.Status != "Pending")
                return false;

            // Check Direct Manager
            if (!await IsManagerAssigned(dto.ApproverId, request, "DirectManager"))
                return false;

            request.Status = "FirstApproved";
            request.CurrentStage = "FirstLevelApproved";
            request.UpdatedAt = DateTime.Now;

            // استخدام الـ History Service الجديد فقط
            await _historyService.AddActionHistory(
       dto.RequestId, dto.RequestType, dto.ApproverId,
       "First Approval", "Pending", "FirstApproved", dto.Comments,dto.requestedBy);

            await _context.SaveChangesAsync();
            return true;
        }

        // ==============================================
        // 🔵 APPROVE SECOND LEVEL
        // InvoiceQ  => FinancialManager
        // Ocity     => ProjectManager
        // ==============================================
        public async Task<bool> ApproveSecondLevel(ApproveRequestDto dto)
        {
            var request = await GetRequest(dto.RequestId, dto.RequestType);
            if (request == null || request.Status != "FirstApproved")
                return false;

            // Determine required role
            string roleNeeded = request switch
            {
                InvoiceQRequest => "GM",
                OcityRequest => "GM",
                _ => null
            };

            if (roleNeeded == null)
                return false;

            // Validate correct manager
            if (!await IsManagerAssigned(dto.ApproverId, request, roleNeeded))
                return false;

            // Update final approval status
            request.Status = "FinalApproved";
            request.CurrentStage = "Completed";
            request.UpdatedAt = DateTime.Now;

            // استخدام الـ History Service الجديد فقط
            await _historyService.AddActionHistory(
      dto.RequestId, dto.RequestType, dto.ApproverId,
      $"Final Approval ({roleNeeded})", "FirstApproved", "FinalApproved", dto.Comments,dto.requestedBy);

            await _context.SaveChangesAsync();
            return true;
        }



        // ==============================================
        // 🔵 Implementor
        // ==============================================
        public async Task<bool> Implementor(ApproveRequestDto dto)
        {
            var request = await GetRequest(dto.RequestId, dto.RequestType);
            if (request == null || request.Status != "FinalApproved")
                return false;

            // Determine required role
            string roleNeeded = request switch
            {
                InvoiceQRequest => "Implementor",
                OcityRequest => "Implementor",
                _ => null
            };

            if (roleNeeded == null)
                return false;

            // Validate correct manager
            if (!await IsManagerAssigned(dto.ApproverId, request, roleNeeded))
                return false;

            // Update Implemented status
            request.Status = "Implemented";
            request.CurrentStage = "Completed";
            request.UpdatedAt = DateTime.Now;

            // استخدام الـ History Service الجديد فقط
            await _historyService.AddActionHistory(
      dto.RequestId, dto.RequestType, dto.ApproverId,
      $"Implemented ({roleNeeded})", "FinalApproved", "Implemented", dto.Comments, dto.requestedBy);

            await _context.SaveChangesAsync();
            return true;
        }



        // ==============================================
        // 🔵 REJECT ANY STAGE
        // ==============================================
        public async Task<bool> Reject(RejectRequestDto dto)
        {
            var request = await GetRequest(dto.RequestId, dto.RequestType);
            if (request == null)
                return false;

            var statusBefore = request.Status;
            request.Status = "Rejected";
            request.CurrentStage = "Rejected";
            request.RejectionReason = dto.Reason;
            request.UpdatedAt = DateTime.Now;

            // استخدام الـ History Service الجديد فقط
            await _historyService.AddActionHistory(
                dto.RequestId, dto.RequestType, dto.ApproverId,
                $"Rejected: {dto.Reason}", statusBefore, "Rejected", dto.AdditionalComments,dto.requestedBy);

            await _context.SaveChangesAsync();
            return true;
        }

        // ==============================================
        // 🔵 SUBMIT NEW REQUEST
        // ==============================================
        public async Task<bool> SubmitRequest(SubmitRequestDto dto)
        {
            IRequestEntity request = dto.RequestType.ToLower() switch
            {
                "invoiceq" => new InvoiceQRequest
                {
                    RequestDetails = dto.RequestDetails,
                    CreatedByUsername = dto.CreatedByUsername,
                    ManagerUsername = dto.ManagerUsername,
                    RequestNumber = GenerateRequestNumber("INV"),
                    CurrentStage = "Submitted"
                },
                "ocity" => new OcityRequest
                {
                    RequestDetails = dto.RequestDetails,
                    CreatedByUsername = dto.CreatedByUsername,
                    ManagerUsername = dto.ManagerUsername,
                    RequestNumber = GenerateRequestNumber("OCITY"),
                    CurrentStage = "Submitted"
                },
                "periodicreview" => new PeriodicReview
                {
                    ReviewDetails = dto.RequestDetails,
                    RequestNumber = GenerateRequestNumber("REVIEW"),
                    CurrentStage = "Submitted"
                },
                "permission" => new PermissionRecord
                {
                    PermissionDetails = dto.RequestDetails,
                    UserId = dto.UserId ?? 0,
                    RequestNumber = GenerateRequestNumber("PERM"),
                    CurrentStage = "Submitted"
                },
                _ => null
            };

            if (request == null) return false;

            // Add to appropriate DbSet
            switch (request)
            {
                case InvoiceQRequest inv:
                    _context.InvoiceQRequests.Add(inv);
                    break;
                case OcityRequest oci:
                    _context.OcityRequests.Add(oci);
                    break;
                case PeriodicReview rev:
                    _context.PeriodicReviews.Add(rev);
                    break;
                case PermissionRecord perm:
                    _context.PermissionRecords.Add(perm);
                    break;
            }

            await _context.SaveChangesAsync();

            await _historyService.AddActionHistory(
     request.Id, dto.RequestType, dto.CreatedByUserId,
     "Request Submitted", null, "Pending", dto.Comments,dto.CreatedByUsername);

            return true;
        }

        // ==============================================
        // 🔵 GET REQUEST ACTION HISTORY
        // ==============================================
        public async Task<List<RequestActionHistoryDto>> GetRequestHistory(int requestId, string requestType)
        {
            return await _historyService.GetRequestHistory(requestId, requestType);
        }

        // ==============================================
        // 🔵 GET REQUEST CURRENT STAGE
        // ==============================================
        public async Task<RequestStageDto?> GetRequestCurrentStage(int requestId, string requestType)
        {
            var request = await GetRequest(requestId, requestType);
            if (request == null)
                return null;

            return new RequestStageDto
            {
                RequestId = requestId,
                RequestType = requestType,
                CurrentStage = request.CurrentStage,
                CurrentStatus = request.Status,
                RequestNumber = request.RequestNumber,
                LastUpdated = request.UpdatedAt ?? request.CreatedAt
            };
        }

        // ==============================================
        // 🔵 UPDATE REQUEST STAGE
        // ==============================================
        public async Task<bool> UpdateRequestStage(UpdateStageDto dto)
        {
            var request = await GetRequest(dto.RequestId, dto.RequestType);
            if (request == null) return false;

            var statusBefore = request.Status;
            request.CurrentStage = dto.NewStage;
            request.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrEmpty(dto.NewStatus))
            {
                request.Status = dto.NewStatus;
            }

            // استخدام الـ History Service الجديد فقط
            await _historyService.AddActionHistory(
     dto.RequestId, dto.RequestType, dto.UpdatedBy,
     $"Stage Updated to {dto.NewStage}", statusBefore, request.Status, dto.Comments,dto.requestedBy);
            await _context.SaveChangesAsync();
            return true;
        }

        // ==============================================
        // 🔵 HELPER METHODS
        // ==============================================

        private async Task<IRequestEntity?> GetRequest(int id, string requestType)
        {
            return requestType.ToLower() switch
            {
                "invoiceq" => await _context.InvoiceQRequests.FirstOrDefaultAsync(r => r.Id == id),
                "ocity" => await _context.OcityRequests.FirstOrDefaultAsync(r => r.Id == id),
                "periodicreview" => await _context.PeriodicReviews.FirstOrDefaultAsync(r => r.Id == id),
                "permission" => await _context.PermissionRecords.FirstOrDefaultAsync(r => r.Id == id),
                _ => null
            };
        }

        private async Task<bool> IsManagerAssigned(int approverId, IRequestEntity request, string role)
        {
            string systemName = request switch
            {
                InvoiceQRequest => "InvoiceQ",
                OcityRequest => "Ocity",
                PeriodicReview => "PeriodicReview",
                PermissionRecord => "Permission",
                _ => null
            };

            if (systemName == null) return false;

            return await _context.ManagerAssignments
                .AnyAsync(m =>
                    m.UserId == approverId &&
                  //  m.SystemName == systemName &&
                    m.RoleType == role
                );
        }

        private string GenerateRequestNumber(string prefix)
        {
            return $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        public async Task<string?> GetRole(int userId)
        {
            return await _context.ManagerAssignments
                .Where(x => x.UserId == userId)
                .Select(x => x.RoleType)
                .FirstOrDefaultAsync();
        }

        public async Task<UserRequestVm?> GetRequestDetails(int id, string system)
        {
            IRequestEntity? r = null;

            // ========== 1) GET BASE REQUEST ==========
            if (system.Equals("InvoiceQ", StringComparison.OrdinalIgnoreCase))
            {
                r = await _context.InvoiceQRequests.FirstOrDefaultAsync(x => x.Id == id);
            }
            else if (system.Equals("Ocity", StringComparison.OrdinalIgnoreCase))
            {
                r = await _context.OcityRequests.FirstOrDefaultAsync(x => x.Id == id);
            }

            if (r == null)
                return null;

            // ========== 2) HISTORY (MANUAL JOIN) ==========
            var history = await _context.RequestActionHistory
                .Where(h => h.RequestId == id && h.RequestType == system)
                .OrderBy(h => h.ActionDate)
                .Select(h => new HistoryItem
                {
                    Timestamp = h.ActionDate,
                    Action = h.Action ?? "",
                    UserName = h.ActionByUser != null
                        ? $"{h.ActionByUser.FullName} ({h.ActionByUser.EmployeeId})"
                        : "Unknown User",
                    Notes = h.Comments ?? ""
                })
                .ToListAsync();

            // ========== 3) RequestedBy / ApprovedBy ==========

            string requestedBy = "";
            string approvedBy = "";

            // requester
            if (!string.IsNullOrEmpty(r.RequestedBy))
            {
                var u = await _context.Users
                    .FirstOrDefaultAsync(x => x.EmployeeId == r.RequestedBy);

                requestedBy = u != null
                    ? $"{u.FullName} ({u.EmployeeId})"
                    : r.RequestedBy;
            }

            // approver (manager)
            if (!string.IsNullOrEmpty(r.ManagerUsername))
            {
                var m = await _context.Users
                    .FirstOrDefaultAsync(x => x.EmployeeId == r.ManagerUsername);

                approvedBy = m != null
                    ? $"{m.FullName} ({m.EmployeeId})"
                    : r.ManagerUsername;
            }

            // ========== 4) BUILD VIEW MODEL ==========
            return new UserRequestVm
            {
                Id = r.Id,
                SystemName = r.SystemName,
                Status = r.Status ?? "",
                CurrentStage = r.CurrentStage ?? "",
                RequestNumber = r.RequestNumber ?? "",
                Details = r.RequestDetails ?? "",
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,

                RequestedBy = requestedBy,
                ApprovedBy = approvedBy,

                RequestType = system,          // ثابت للفرونت
                PermissionLevel = "",          // انت ممكن تستخدمها لاحقاً
                Permissions = new List<string>(),

                History = history,
                Comments = new List<Comment>()  // لو لاحقاً هتزود comments
            };
        }


        public async Task<List<UserRequestVm>> GetManagerHistory(int userId)
        {
            // 1) هات دور المدير
            var role = await GetRole(userId);

            if (string.IsNullOrEmpty(role))
                return new List<UserRequestVm>();

            // 2) هات Username بتاع المستخدم
            var username = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.EmployeeId)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(username))
                return new List<UserRequestVm>();


            var all = new List<IRequestEntity>();


            // =====================
            //    DIRECT MANAGER
            // =====================
            if (role == "DirectManager")
            {
                var ocity = await _context.OcityRequests
                    .Where(r => r.ManagerUsername == username)
                    .ToListAsync();

                var invoice = await _context.InvoiceQRequests
                    .Where(r => r.ManagerUsername == username)
                    .ToListAsync();

                all.AddRange(ocity);
                all.AddRange(invoice);
            }


            // =====================
            //        GM
            // =====================
            else if (role == "GM" || role == "FinalManager")
            {
                var ocity = await _context.OcityRequests
                    .Where(r => r.CurrentStage == "FinalApproval"
                             || r.Status == "Approved"
                             || r.Status == "FinalApproved")
                    .ToListAsync();

                var invoice = await _context.InvoiceQRequests
                    .Where(r => r.CurrentStage == "FinalApproval"
                             || r.Status == "Approved"
                             || r.Status == "FinalApproved")
                    .ToListAsync();

                all.AddRange(ocity);
                all.AddRange(invoice);
            }


            // =====================
            //       MAP TO VM
            // =====================
            return all
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new UserRequestVm
                {
                    Id = r.Id,
                    SystemName = r.SystemName,
                    Status = r.Status,
                    CurrentStage = r.CurrentStage,
                    RequestNumber = r.RequestNumber,
                    Details = r.RequestDetails,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToList();
        }

    }
}