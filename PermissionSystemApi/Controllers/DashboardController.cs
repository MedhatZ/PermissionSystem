using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PermissionSystemApi.Data;
using PermissionSystemApi.Dtos;
using PermissionSystemApi.Models;
using PermissionSystemApi.Services;
using System.Security.Claims;

namespace PermissionSystem.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ApprovalService _approvalService;

        public DashboardController(ApplicationDbContext context, ApprovalService approvalService)
        {
            _context = context;
            _approvalService = approvalService;

        }



        [HttpGet("stats")]
        public IActionResult GetStats([FromQuery] string userId = null, [FromQuery] string empId = null)
        {
            // التحقق من وجود userId
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required");
            }

            // التحقق إذا كان userId رقم صحيح
            if (!int.TryParse(userId, out int parsedUserId))
            {
                return BadRequest("Invalid User ID");
            }

            var role = _context.ManagerAssignments
                .Where(x => x.UserId == parsedUserId)
                .Select(x => x.RoleType)
                .FirstOrDefault();

            if (role == "GM")
            {
                // إحصائيات المدير العام (كل البيانات)
                var stats = new
                {
                    totalInvoiceQ = _context.InvoiceQRequests.Count(),
                    totalOcity = _context.OcityRequests.Count(),
                    totalPeriodicReviews = _context.PeriodicReviews.Count(),
                    totalPermissions = _context.PermissionRecords.Count(),

                    pendingFirst = _context.InvoiceQRequests.Count(r => r.Status == "Pending")
                                + _context.OcityRequests.Count(r => r.Status == "Pending"),
                    pendingFinal = _context.InvoiceQRequests.Count(r => r.Status == "FirstApproved")
                                + _context.OcityRequests.Count(r => r.Status == "FirstApproved"),

                    approved = _context.InvoiceQRequests.Count(r => r.Status == "FinalApproved")
                             + _context.OcityRequests.Count(r => r.Status == "FinalApproved"),
                    rejected = _context.InvoiceQRequests.Count(r => r.Status == "Rejected")
                             + _context.OcityRequests.Count(r => r.Status == "Rejected"),

                    approvedToday = _context.InvoiceQRequests.Count(r =>
                                    (r.Status == "FirstApproved" || r.Status == "FinalApproved")
                                    && r.UpdatedAt != null
                                    && r.UpdatedAt.Value.Date == DateTime.UtcNow.Date)
                                 + _context.OcityRequests.Count(r =>
                                    (r.Status == "FirstApproved" || r.Status == "FinalApproved")
                                    && r.UpdatedAt != null
                                    && r.UpdatedAt.Value.Date == DateTime.UtcNow.Date)
                };
                return Ok(stats);
            }
            else if (role == "DirectManager")
            {
                // إحصائيات المدير المباشر (البيانات التي تحتاج موافقته فقط)
                var stats = new
                {
                    totalInvoiceQ = _context.InvoiceQRequests.Count(r => r.ManagerUsername == empId),
                    totalOcity = _context.OcityRequests.Count(r => r.ManagerUsername == empId),
                    totalPeriodicReviews = 0, // المدير المباشر مالهش علاقة بالمراجعات الدورية
                    totalPermissions = 0,     // المدير المباشر مالهش علاقة بالصلاحيات

                    pendingFirst = _context.InvoiceQRequests.Count(r => r.ManagerUsername == empId && r.Status == "Pending")
                                + _context.OcityRequests.Count(r => r.ManagerUsername == empId && r.Status == "Pending"),
                    pendingFinal = _context.InvoiceQRequests.Count(r => r.ManagerUsername == empId && r.Status == "FirstApproved")
                                + _context.OcityRequests.Count(r => r.ManagerUsername == empId && r.Status == "FirstApproved"),

                    approved = _context.InvoiceQRequests.Count(r => r.ManagerUsername == empId && r.Status == "FinalApproved")
                             + _context.OcityRequests.Count(r => r.ManagerUsername == empId && r.Status == "FinalApproved"),
                    rejected = _context.InvoiceQRequests.Count(r => r.ManagerUsername == empId && r.Status == "Rejected")
                             + _context.OcityRequests.Count(r => r.ManagerUsername == empId && r.Status == "Rejected"),

                    approvedToday = _context.InvoiceQRequests.Count(r => r.ManagerUsername == empId &&
                                        (r.Status == "FirstApproved" || r.Status == "FinalApproved")
                                        && r.UpdatedAt != null
                                        && r.UpdatedAt.Value.Date == DateTime.UtcNow.Date)
                                 + _context.OcityRequests.Count(r => r.ManagerUsername == empId &&
                                        (r.Status == "FirstApproved" || r.Status == "FinalApproved")
                                        && r.UpdatedAt != null
                                        && r.UpdatedAt.Value.Date == DateTime.UtcNow.Date)
                };
                return Ok(stats);
            }
            else
            {
                // حالة احتياطية - مش متوقعة لكن علشان safety
                return BadRequest("Unauthorized access");
            }
        }
        [HttpGet("chart")]
        public IActionResult GetChart()
        {
            var invoice = _context.InvoiceQRequests
                .GroupBy(r => r.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToList();

            var ocity = _context.OcityRequests
                .GroupBy(r => r.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToList();

            var periodic = _context.PeriodicReviews
                .GroupBy(r => r.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToList();

            var permissions = _context.PermissionRecords
                .GroupBy(r => r.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToList();

            return Ok(new
            {
                invoiceCount = invoice,
                ocityCount = ocity,
                periodicCount = periodic,
                permissionsCount = permissions
            });
        }


        [HttpGet("recent")]
        public IActionResult GetRecentRequests()
        {
            var results = new List<object>();

            try
            {
                // ✅ InvoiceQ - فيه الأعمدة
                var invoice = _context.InvoiceQRequests
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new {
                        id = x.Id,
                        system = "InvoiceQ",
                        status = x.Status ?? "",
                        currentStage = x.CurrentStage ?? "N/A",
                        requestNumber = x.RequestNumber ?? "N/A",
                        createdAt = x.CreatedAt,
                        details = x.RequestDetails ?? ""
                    })
                    .Take(10)
                    .ToList();

                results.AddRange(invoice);
            }
            catch { /* تجاهل الخطأ */ }

            try
            {
                // ✅ Ocity - فيه الأعمدة  
                var ocity = _context.OcityRequests
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new {
                        id = x.Id,
                        system = "Ocity",
                        status = x.Status ?? "",
                        currentStage = x.CurrentStage ?? "N/A",
                        requestNumber = x.RequestNumber ?? "N/A",
                        createdAt = x.CreatedAt,
                        details = x.RequestDetails ?? ""
                    })
                    .Take(10)
                    .ToList();

                results.AddRange(ocity);
            }
            catch { /* تجاهل الخطأ */ }

            try
            {
                // ❌ PeriodicReviews - ممكن مافيهاش الأعمدة
                var periodic = _context.PeriodicReviews
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new {
                        id = x.Id,
                        system = "PeriodicReview",
                        status = x.Status ?? "",
                        currentStage = "N/A",
                        requestNumber = "N/A",
                        createdAt = x.CreatedAt,
                        details = x.ReviewDetails ?? ""
                    })
                    .Take(10)
                    .ToList();

                results.AddRange(periodic);
            }
            catch { /* تجاهل الخطأ */ }

            try
            {
                // ❌ PermissionRecords - ممكن مافيهاش الأعمدة
                var permissions = _context.PermissionRecords
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new {
                        id = x.Id,
                        system = "Permission",
                        status = x.Status ?? "",
                        currentStage = "N/A",
                        requestNumber = "N/A",
                        createdAt = x.CreatedAt,
                        details = x.PermissionDetails ?? ""
                    })
                    .Take(10)
                    .ToList();

                results.AddRange(permissions);
            }
            catch { /* تجاهل الخطأ */ }

            // ترتيب النتائج النهائية
            var all = results
                .OrderByDescending(x => ((dynamic)x).createdAt)
                .Take(10)
                .ToList();

            return Ok(all);
        }

        [HttpGet("pending-approvals/{userId}")]
        public IActionResult GetPendingApprovals(int userId)
        {
            var pending = new List<PendingApprovalVm>();

            // Direct Manager
            var direct = _context.ManagerAssignments
                .Where(m => m.UserId == userId && m.RoleType == "DirectManager")
                .ToList();

            foreach (var m in direct)
            {
                if (m.SystemName == "InvoiceQ")
                {
                    pending.AddRange(
                        _context.InvoiceQRequests
                        .Where(r => r.Status == "Pending")
                        .Select(r => new PendingApprovalVm
                        {
                            system = "InvoiceQ",
                            id = r.Id,
                            requestNumber = r.RequestNumber,
                            status = r.Status,
                            currentStage = r.CurrentStage,
                            createdAt = r.CreatedAt,
                            details = r.RequestDetails,
                            role = "First Approval"
                        })
                        .ToList()
                    );
                }
                else if (m.SystemName == "Ocity")
                {
                    pending.AddRange(
                        _context.OcityRequests
                        .Where(r => r.Status == "Pending")
                        .Select(r => new PendingApprovalVm
                        {
                            system = "Ocity",
                            id = r.Id,
                            requestNumber = r.RequestNumber,
                            status = r.Status,
                            currentStage = r.CurrentStage,
                            createdAt = r.CreatedAt,
                            details = r.RequestDetails,
                            role = "First Approval"
                        })
                        .ToList()
                    );
                }
            }

            // Final Managers (Financial / Project)
            var finalMgr = _context.ManagerAssignments
                .Where(m => m.UserId == userId &&
                       (m.RoleType == "FinancialManager" || m.RoleType == "ProjectManager"))
                .ToList();

            foreach (var m in finalMgr)
            {
                if (m.SystemName == "InvoiceQ" && m.RoleType == "FinancialManager")
                {
                    pending.AddRange(
                        _context.InvoiceQRequests
                        .Where(r => r.Status == "FirstApproved")
                        .Select(r => new PendingApprovalVm
                        {
                            system = "InvoiceQ",
                            id = r.Id,
                            requestNumber = r.RequestNumber,
                            status = r.Status,
                            currentStage = r.CurrentStage,
                            createdAt = r.CreatedAt,
                            details = r.RequestDetails,
                            role = "Final Approval (Financial)"
                        })
                        .ToList()
                    );
                }
                else if (m.SystemName == "Ocity" && m.RoleType == "ProjectManager")
                {
                    pending.AddRange(
                        _context.OcityRequests
                        .Where(r => r.Status == "FirstApproved")
                        .Select(r => new PendingApprovalVm
                        {
                            system = "Ocity",
                            id = r.Id,
                            requestNumber = r.RequestNumber,
                            status = r.Status,
                            currentStage = r.CurrentStage,
                            createdAt = r.CreatedAt,
                            details = r.RequestDetails,
                            role = "Final Approval (Project)"
                        })
                        .ToList()
                    );
                }
            }

            return Ok(pending.OrderByDescending(x => x.createdAt).Take(10));
        }

        [HttpGet("activity")]
        public IActionResult GetActivity()
        {
            var logs = _context.RequestActionHistory
                .OrderByDescending(x => x.ActionDate)
                .Take(10)
                .Include(x => x.ActionByUser)
                .Select(l => new {
                    user = l.ActionByUser.FullName ?? l.ActionByUser.EmployeeId,
                    action = l.Action,
                    requestType = l.RequestType,
                    requestId = l.RequestId,
                    statusBefore = l.StatusBefore,
                    statusAfter = l.StatusAfter,
                    comments = l.Comments,
                    actionDate = l.ActionDate
                })
                .ToList();

            return Ok(logs);
        }
    }

   
}