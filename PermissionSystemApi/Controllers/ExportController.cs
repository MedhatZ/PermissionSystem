using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PermissionSystemApi.Data;
using PermissionSystemApi.Services;

namespace PermissionSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExportController : ControllerBase
    {
        private readonly RequestService _requestService;
        private readonly ApplicationDbContext _context;

        public ExportController(RequestService requestService, ApplicationDbContext context)
        {
            _requestService = requestService;
            _context = context;
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportAll()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.AddWorksheet("Requests");

            // Headers
            sheet.Cell(1, 1).Value = "Request Number";
            sheet.Cell(1, 2).Value = "System";
            sheet.Cell(1, 3).Value = "Details";
            sheet.Cell(1, 4).Value = "Status";
            sheet.Cell(1, 5).Value = "Current Stage";
            sheet.Cell(1, 6).Value = "Created By";
            sheet.Cell(1, 7).Value = "Manager";
            sheet.Cell(1, 8).Value = "Created At";
            sheet.Cell(1, 9).Value = "Updated At";
            sheet.Cell(1, 10).Value = "Rejection Reason";

            var invoice = await _context.InvoiceQRequests.ToListAsync();
            var ocity = await _context.OcityRequests.ToListAsync();
            var periodic = await _context.PeriodicReviews.ToListAsync();
            var permissions = await _context.PermissionRecords.ToListAsync();

            int row = 2;

            // InvoiceQ Requests
            foreach (var r in invoice)
            {
                sheet.Cell(row, 1).Value = r.RequestNumber;
                sheet.Cell(row, 2).Value = "InvoiceQ";
                sheet.Cell(row, 3).Value = r.RequestDetails;
                sheet.Cell(row, 4).Value = r.Status;
                sheet.Cell(row, 5).Value = r.CurrentStage;
                sheet.Cell(row, 6).Value = r.CreatedByUsername;
                sheet.Cell(row, 7).Value = r.ManagerUsername;
                sheet.Cell(row, 8).Value = r.CreatedAt;
                sheet.Cell(row, 9).Value = r.UpdatedAt;
                sheet.Cell(row, 10).Value = r.RejectionReason;
                row++;
            }

            // Ocity Requests
            foreach (var r in ocity)
            {
                sheet.Cell(row, 1).Value = r.RequestNumber;
                sheet.Cell(row, 2).Value = "Ocity";
                sheet.Cell(row, 3).Value = r.RequestDetails;
                sheet.Cell(row, 4).Value = r.Status;
                sheet.Cell(row, 5).Value = r.CurrentStage;
                sheet.Cell(row, 6).Value = r.CreatedByUsername;
                sheet.Cell(row, 7).Value = r.ManagerUsername;
                sheet.Cell(row, 8).Value = r.CreatedAt;
                sheet.Cell(row, 9).Value = r.UpdatedAt;
                sheet.Cell(row, 10).Value = r.RejectionReason;
                row++;
            }

            // Periodic Reviews
            foreach (var r in periodic)
            {
                sheet.Cell(row, 1).Value = r.RequestNumber;
                sheet.Cell(row, 2).Value = "Periodic Review";
                sheet.Cell(row, 3).Value = r.ReviewDetails;
                sheet.Cell(row, 4).Value = r.Status;
                sheet.Cell(row, 5).Value = r.CurrentStage;
                sheet.Cell(row, 6).Value = "System";
                sheet.Cell(row, 7).Value = "N/A";
                sheet.Cell(row, 8).Value = r.CreatedAt;
                sheet.Cell(row, 9).Value = r.UpdatedAt;
                sheet.Cell(row, 10).Value = r.RejectionReason;
                row++;
            }

            // Permission Records
            foreach (var r in permissions)
            {
                sheet.Cell(row, 1).Value = r.RequestNumber;
                sheet.Cell(row, 2).Value = "Permission";
                sheet.Cell(row, 3).Value = r.PermissionDetails;
                sheet.Cell(row, 4).Value = r.Status;
                sheet.Cell(row, 5).Value = r.CurrentStage;
                sheet.Cell(row, 6).Value = r.UserId.ToString();
                sheet.Cell(row, 7).Value = "N/A";
                sheet.Cell(row, 8).Value = r.CreatedAt;
                sheet.Cell(row, 9).Value = r.UpdatedAt;
                sheet.Cell(row, 10).Value = r.RejectionReason;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"All_Requests_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }

        [HttpGet("export-action-history")]
        public async Task<IActionResult> ExportActionHistory()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.AddWorksheet("Action History");

            // Headers
            sheet.Cell(1, 1).Value = "Request ID";
            sheet.Cell(1, 2).Value = "Request Type";
            sheet.Cell(1, 3).Value = "Action";
            sheet.Cell(1, 4).Value = "Action By";
            sheet.Cell(1, 5).Value = "Action Date";
            sheet.Cell(1, 6).Value = "Comments";
            sheet.Cell(1, 7).Value = "Status Before";
            sheet.Cell(1, 8).Value = "Status After";

            var history = await _context.RequestActionHistory
                .Include(h => h.ActionByUser)
                .OrderByDescending(h => h.ActionDate)
                .ToListAsync();

            int row = 2;

            foreach (var h in history)
            {
                sheet.Cell(row, 1).Value = h.RequestId;
                sheet.Cell(row, 2).Value = h.RequestType;
                sheet.Cell(row, 3).Value = h.Action;
                sheet.Cell(row, 4).Value = h.ActionByUser.FullName ?? h.ActionByUser.EmployeeId;
                sheet.Cell(row, 5).Value = h.ActionDate;
                sheet.Cell(row, 6).Value = h.Comments;
                sheet.Cell(row, 7).Value = h.StatusBefore;
                sheet.Cell(row, 8).Value = h.StatusAfter;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Action_History_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }
    }
}