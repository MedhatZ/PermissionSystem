using Microsoft.EntityFrameworkCore;
using PermissionSystemApi.Data;
using PermissionSystemApi.Dtos;
using PermissionSystemApi.Models;

namespace PermissionSystemApi.Services
{
    public class RequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // ============================================
        // 🔵 CREATE InvoiceQ REQUEST
        // ============================================
        public async Task<InvoiceQRequest> CreateInvoiceQRequest(CreateInvoiceQRequestDto dto)
        {
            var username = _httpContextAccessor.HttpContext.User.FindFirst("employeeId")?.Value;
            var manager = _httpContextAccessor.HttpContext.User.FindFirst("managerId")?.Value;

            var request = new InvoiceQRequest
            {
                RequestDetails = $"Name: {dto.Name}, Mobile: {dto.MobileNumber}, Email: {dto.Email}, Department: {dto.Department}, JobTitle: {dto.JobTitle}, Unit: {dto.Unit}",
                Status = "Pending",
                CurrentStage = "Submitted",
                RequestNumber = GenerateRequestNumber("INV"),
                CreatedAt = DateTime.Now,
                CreatedByUsername = username,
                ManagerUsername = manager
            };

            _context.InvoiceQRequests.Add(request);
            await _context.SaveChangesAsync();

            await AssignRequestToDirectManagers("InvoiceQ", request.Id);
            return request;
        }

        // ============================================
        // 🔵 CREATE Ocity REQUEST
        // ============================================
        public async Task<OcityRequest> CreateOcityRequest(CreateOcityRequestDto dto)
        {
            var username = _httpContextAccessor.HttpContext.User.FindFirst("employeeId")?.Value;
            var manager = _httpContextAccessor.HttpContext.User.FindFirst("managerId")?.Value;

            var request = new OcityRequest
            {
                RequestDetails = $"Name: {dto.Name}, Mobile: {dto.MobileNumber}, Email: {dto.Email}, Project: {dto.Project}, JobTitle: {dto.JobTitle}",
                Status = "Pending",
                CurrentStage = "Submitted",
                RequestNumber = GenerateRequestNumber("OCITY"),
                CreatedAt = DateTime.Now,
                CreatedByUsername = username,
                ManagerUsername = manager
            };

            _context.OcityRequests.Add(request);
            await _context.SaveChangesAsync();

            await AssignRequestToDirectManagers("Ocity", request.Id);
            return request;
        }

        // ==================================================
        // 🔵 GET Pending Requests FOR Direct Manager
        // ==================================================
        public async Task<List<IRequestEntity>> GetPendingForDirectManager(int userId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return new List<IRequestEntity>();

                var list = new List<IRequestEntity>();

                var ocity = await _context.OcityRequests
                    .Where(r => r.ManagerUsername == user.EmployeeId && r.Status == "Pending")
                    .ToListAsync();

                var invoiceQ = await _context.InvoiceQRequests
                    .Where(r => r.ManagerUsername == user.EmployeeId && r.Status == "Pending")
                    .ToListAsync();

                list.AddRange(ocity);
                list.AddRange(invoiceQ);

                return list;
            }
            catch (Exception ex)
            {

                throw;
            }
          
        }

        public async Task<List<RequestItemDto>> GetAllForUser(string employeeId)
        {
            var result = new List<RequestItemDto>();

            var invoiceQ = await _context.InvoiceQRequests
                .Where(r => r.CreatedByUsername == employeeId)
                .ToListAsync();

            result.AddRange(invoiceQ.Select(r => new RequestItemDto
            {
                Id = r.Id,
                SystemName = "InvoiceQ",
                RequestDetails = r.RequestDetails,
                Status = r.Status,
                CurrentStage = r.CurrentStage,
                CreatedAt = r.CreatedAt,
                RequestNumber = r.RequestNumber
            }));

            var ocity = await _context.OcityRequests
                .Where(r => r.CreatedByUsername == employeeId)
                .ToListAsync();

            result.AddRange(ocity.Select(r => new RequestItemDto
            {
                Id = r.Id,
                SystemName = "Ocity",
                RequestDetails = r.RequestDetails,
                Status = r.Status,
                CurrentStage = r.CurrentStage,
                CreatedAt = r.CreatedAt,
                RequestNumber = r.RequestNumber
            }));

            return result;
        }

        // ==================================================
        // 🔵 GET Pending Second Approval
        // ==================================================
        public async Task<List<IRequestEntity>> GetPendingSecondLevel(int userId)
        {
            var hasProjectManagerRole = await _context.ManagerAssignments
                .AnyAsync(m => m.UserId == userId && m.RoleType == "GM");

            var result = new List<IRequestEntity>();

            if (hasProjectManagerRole)
            {
                var invoiceQ = await _context.InvoiceQRequests
                    .Where(r => r.Status == "FirstApproved")
                    .ToListAsync();

                var ocity = await _context.OcityRequests
                    .Where(r => r.Status == "FirstApproved")
                    .ToListAsync();

                result.AddRange(invoiceQ);
                result.AddRange(ocity);
            }

            return result;
        }

        // ==================================================
        // 🔵 GET Pending Implementor
        // ==================================================
        public async Task<List<IRequestEntity>> GetPendingImplementor(int userId)
        {
            var hasProjectManagerRole = await _context.ManagerAssignments
                .AnyAsync(m => m.UserId == userId && m.RoleType == "Implementor");

            var result = new List<IRequestEntity>();

            if (hasProjectManagerRole)
            {
                var invoiceQ = await _context.InvoiceQRequests
                    .Where(r => r.Status == "FinalApproved")
                    .ToListAsync();

                var ocity = await _context.OcityRequests
                    .Where(r => r.Status == "FinalApproved")
                    .ToListAsync();

                result.AddRange(invoiceQ);
                result.AddRange(ocity);
            }

            return result;
        }

        public async Task<List<RequestItemDto>> GetAllInvoiceQAsync()
        {
            return await _context.InvoiceQRequests
                .Select(r => new RequestItemDto
                {
                    Id = r.Id,
                    RequestDetails = r.RequestDetails,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    RequestNumber = r.RequestNumber
                }).ToListAsync();
        }

        public async Task<List<RequestItemDto>> GetAllOcityAsync()
        {
            return await _context.OcityRequests
                .Select(r => new RequestItemDto
                {
                    Id = r.Id,
                    RequestDetails = r.RequestDetails,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    RequestNumber = r.RequestNumber
                }).ToListAsync();
        }

        public async Task<UserRequestVm?> GetRequestDetails(int id, string system)
        {
            IRequestEntity? r = null;

            // 1) Get main request record
            if (system.Equals("InvoiceQ", StringComparison.OrdinalIgnoreCase))
            {
                r = await _context.InvoiceQRequests
                    .FirstOrDefaultAsync(x => x.Id == id);
            }
            else if (system.Equals("Ocity", StringComparison.OrdinalIgnoreCase))
            {
                r = await _context.OcityRequests
                    .FirstOrDefaultAsync(x => x.Id == id);
            }

            if (r == null) return null;

            // 2) Load complete history INCLUDING user details
            var history = new List<RequestActionHistory>();
            if (system == "InvoiceQ")
            {
                 history = await _context.RequestActionHistory
                              .Where(a => a.InvoiceQRequestId == id )
                              .Include(a => a.ActionByUser)
                              .OrderBy(a => a.ActionDate)
                              .ToListAsync();
            }else {
                 history = await _context.RequestActionHistory
                       .Where(a => a.RequestId == id )
                       .Include(a => a.ActionByUser)
                       .OrderBy(a => a.ActionDate)
                       .ToListAsync();
            }
               

            // 3) Safe mapping — no null exceptions ever
            var historyVm = history.Select(a => new HistoryItem
            {
                Timestamp = a.ActionDate,
                Action = a.Action ?? string.Empty,
                Notes = a.Comments ?? string.Empty,
                UserName = a.ActionByUser != null
                    ? $"{a.ActionByUser.FullName} ({a.ActionByUser.EmployeeId})"
                    : $"User #{a.ActionBy}"   // fallback
            }).ToList();

            // 4) Return final VM مع كل الحقول الجديدة
            var vm = new UserRequestVm
            {
                Id = r.Id,
                SystemName = r.SystemName,
                Status = r.Status ?? string.Empty,
                Details = r.RequestDetails ?? string.Empty,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                RequestNumber = r.RequestNumber ?? string.Empty,
                CurrentStage = r.CurrentStage ?? string.Empty,
                History = historyVm ?? new List<HistoryItem>(), // safe fallback
                RequestedBy = r.RequestedBy,
                ApprovedBy = null, // يمكن تضيفه من الـ history إذا محتاج
                PermissionLevel = string.Empty,
                Permissions = new List<string>(),
                Comments = new List<Comment>(),
                email= r.email ?? string.Empty,
                empNumber= r.empNumber ?? string.Empty,
            };

            // 🔵 إضافة الحقول الجديدة بناءً على نوع الـ Request
            if (system.Equals("InvoiceQ", StringComparison.OrdinalIgnoreCase) && r is InvoiceQRequest invoiceQ)
            {
                vm.TaskDetail = invoiceQ.TaskDetail ?? string.Empty;
                vm.RequestType = invoiceQ.RequestType ?? string.Empty;
                vm.UserModificationDate = invoiceQ.UserModificationDate;
                vm.UpdateType = invoiceQ.UpdateType;
                vm.UpdateDate = invoiceQ.UpdateDate;
                vm.RoleModified = invoiceQ.RoleModified;
                vm.CreatedByUsername = invoiceQ.CreatedByUsername;
                vm.ManagerUsername = invoiceQ.ManagerUsername;
            }
            else if (system.Equals("Ocity", StringComparison.OrdinalIgnoreCase) && r is OcityRequest ocity)
            {
                vm.TaskDetail = ocity.TaskDetail ?? string.Empty;
                vm.RequestType = ocity.RequestType ?? string.Empty;
                vm.UserModificationDate = ocity.UserModificationDate;
                vm.UpdateType = ocity.UpdateType;
                vm.UpdateDate = ocity.UpdateDate;
                vm.RoleModified = ocity.RoleModified;
                vm.CreatedByUsername = ocity.CreatedByUsername;
                vm.ManagerUsername = ocity.ManagerUsername;
            }

            return vm;
        }
        // ==================================================
        // 🔵 HELPER METHODS
        // ==================================================
        private async Task AssignRequestToDirectManagers(string systemName, int requestId)
        {
            var directManagers = await _context.ManagerAssignments
                .Where(m => m.SystemName == systemName && m.RoleType == "DirectManager")
                .ToListAsync();
        }

        private string GenerateRequestNumber(string prefix)
        {
            return $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
    }
}