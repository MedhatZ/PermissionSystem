using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PermissionSystemApi.Data;
using PermissionSystemApi.Dtos;
using PermissionSystemApi.Models;
using PermissionSystemApi.Services;
using System.Text.RegularExpressions;

namespace PermissionSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RequestsController : ControllerBase
    {
        private readonly RequestService _requestService;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestsController(RequestService requestService, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _requestService = requestService;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // ============================================
        // CREATE InvoiceQ
        // ============================================
        [HttpPost("invoiceq")]
        public async Task<IActionResult> CreateInvoiceQ([FromBody] CreateInvoiceQRequestDto dto)
        {
            var result = await _requestService.CreateInvoiceQRequest(dto);
            return Ok(result);
        }

        // ============================================
        // CREATE Ocity
        // ============================================
        [HttpPost("ocity")]
        public async Task<IActionResult> CreateOcity([FromBody] CreateOcityRequestDto dto)
        {
            var result = await _requestService.CreateOcityRequest(dto);
            return Ok(result);
        }

        // ============================================
        // GET Pending for Direct Manager
        // ============================================
        [HttpGet("pending/direct")]
        public async Task<IActionResult> GetPendingDirect([FromQuery] int userId)
        {
            var result = await _requestService.GetPendingForDirectManager(userId);
             
            return Ok(result);
        }

        // ============================================
        // GET Pending for Final Approver
        // ============================================
        [HttpGet("pending/final")]
        public async Task<IActionResult> GetPendingFinal([FromQuery] int userId)
        {
            var result = await _requestService.GetPendingSecondLevel(userId);
            return Ok(result);
        }

        // ============================================
        // GET Pending for Implementor
        // ============================================
        [HttpGet("pending/Implementor")]
        public async Task<IActionResult> GetPendingImplementor([FromQuery] int userId)
        {
            var result = await _requestService.GetPendingImplementor(userId);
            return Ok(result);
        }



        // ============================
        // GET ALL INVOICEQ
        // ============================
        [HttpGet("all/invoiceq")]
        public async Task<IActionResult> GetAllInvoiceQ()
        {
            var result = await _requestService.GetAllInvoiceQAsync();
            return Ok(result);
        }

        // ============================
        // GET ALL OCITY
        // ============================
        [HttpGet("all/ocity")]
        public async Task<IActionResult> GetAllOcity()
        {
            var result = await _requestService.GetAllOcityAsync();
            return Ok(result);
        }

        [HttpPost("bulk-upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> BulkUpload([FromForm] BulkUploadDto dto)
        {
            var file = dto.File;
            var systemName = dto.systemType;

            if (file == null || file.Length == 0)
                return BadRequest("Invalid file.");

            if (systemName != "InvoiceQ" && systemName != "Ocity")
                return BadRequest("Invalid system name.");

            // =====================================================
            // 🔵 Get user + manager from JWT
            // =====================================================
            var employeeId = _httpContextAccessor.HttpContext.User.FindFirst("employeeId")?.Value;
            var managerId = _httpContextAccessor.HttpContext.User.FindFirst("managerId")?.Value;

            if (string.IsNullOrEmpty(employeeId))
                return Unauthorized("Invalid JWT or missing employeeId claim.");

            // Get user details for username display
            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
            var username = employeeId;
            var manager = managerId;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);

            // 🔵 استخدام الـ worksheet بناءً على systemName
            var sheet = workbook.Worksheet(systemName);
            if (sheet == null)
            {
                return BadRequest($"No '{systemName}' worksheet found in the Excel file.");
            }

            var rows = sheet.RangeUsed().RowsUsed().Skip(1); // Skip Header
            int success = 0;
            int failed = 0;

            var errors = new List<string>();

            HashSet<string> emailSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                try
                {
                    string name = row.Cell(1).GetString().Trim();
                    string mobile = row.Cell(2).GetString().Trim();
                    string email = row.Cell(3).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        failed++;
                        errors.Add($"Row {row.RowNumber()}: Name is required.");
                        continue;
                    }

                    if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    {
                        failed++;
                        errors.Add($"Row {row.RowNumber()}: Invalid email format.");
                        continue;
                    }

                    if (!emailSet.Add(email))
                    {
                        failed++;
                        errors.Add($"Row {row.RowNumber()}: Duplicate email inside the file.");
                        continue;
                    }

                    if (systemName == "InvoiceQ")
                    {
                        string department = row.Cell(4).GetString().Trim();
                        string jobTitle = row.Cell(5).GetString().Trim();
                        string unit = row.Cell(6).GetString().Trim();
                        string taskDetail = row.Cell(7).GetString().Trim();
                        string requestType = row.Cell(8).GetString().Trim();
                        string userModificationDateStr = row.Cell(9).GetString().Trim();
                        string updateType = row.Cell(10).GetString().Trim();
                        string updateDateStr = row.Cell(11).GetString().Trim();
                        string roleModified = row.Cell(12).GetString().Trim();

                        DateTime? userModificationDate = null;
                        DateTime? updateDate = null;

                        if (DateTime.TryParse(userModificationDateStr, out DateTime tempDate1))
                            userModificationDate = tempDate1;

                        if (DateTime.TryParse(updateDateStr, out DateTime tempDate2))
                            updateDate = tempDate2;


                        if (string.IsNullOrWhiteSpace(department) ||
                            string.IsNullOrWhiteSpace(jobTitle) ||
                            string.IsNullOrWhiteSpace(unit) ||
                            string.IsNullOrWhiteSpace(taskDetail) ||
                            string.IsNullOrWhiteSpace(requestType))
                        {
                            failed++;
                            errors.Add($"Row {row.RowNumber()}: Missing InvoiceQ fields.");
                            continue;
                        }

                        // 🔵 التحقق من صحة RequestType
                        //if (!IsValidRequestType(requestType))
                        //{
                        //    failed++;
                        //    errors.Add($"Row {row.RowNumber()}: Invalid RequestType '{requestType}'. Must be: Create, Modify, or Disable");
                        //    continue;
                        //}

                        _context.InvoiceQRequests.Add(new InvoiceQRequest
                        {
                            RequestDetails = $"Name: {name}, Mobile: {mobile}, Email: {email}, Department: {department}, JobTitle: {jobTitle}, Unit: {unit}",
                            TaskDetail = taskDetail,
                            RequestType = requestType,
                            Status = "Pending",
                            CurrentStage = "Submitted",
                            RequestNumber = GenerateRequestNumber("INV"),
                            CreatedAt = DateTime.Now,
                            CreatedByUsername = username,
                            ManagerUsername = manager,
                            UserModificationDate = userModificationDate,
                            UpdateType = updateType,
                            UpdateDate = updateDate,
                            RoleModified = roleModified
                        });
                    }
                    else if (systemName == "Ocity")
                    {
                        string jobTitle = row.Cell(4).GetString().Trim();
                        string project = row.Cell(5).GetString().Trim();
                        string taskDetail = row.Cell(6).GetString().Trim();
                        string requestType = row.Cell(7).GetString().Trim();
                        string userModificationDateStr = row.Cell(9).GetString().Trim();
                        string updateType = row.Cell(10).GetString().Trim();
                        string updateDateStr = row.Cell(11).GetString().Trim();
                        string roleModified = row.Cell(12).GetString().Trim();

                        DateTime? userModificationDate = null;
                        DateTime? updateDate = null;

                        if (DateTime.TryParse(userModificationDateStr, out DateTime tempDate1))
                            userModificationDate = tempDate1;

                        if (DateTime.TryParse(updateDateStr, out DateTime tempDate2))
                            updateDate = tempDate2;



                        if (string.IsNullOrWhiteSpace(project) ||
                            string.IsNullOrWhiteSpace(jobTitle) ||
                            string.IsNullOrWhiteSpace(taskDetail) ||
                            string.IsNullOrWhiteSpace(requestType))
                        {
                            failed++;
                            errors.Add($"Row {row.RowNumber()}: Missing Ocity fields.");
                            continue;
                        }

                        // 🔵 التحقق من صحة RequestType
                        //if (!IsValidRequestType(requestType))
                        //{
                        //    failed++;
                        //    errors.Add($"Row {row.RowNumber()}: Invalid RequestType '{requestType}'. Must be: Create, Modify, or Disable");
                        //    continue;
                        //}

                        _context.OcityRequests.Add(new OcityRequest
                        {
                            RequestDetails = $"Name: {name}, Mobile: {mobile}, Email: {email}, Project: {project}, JobTitle: {jobTitle}",
                            TaskDetail = taskDetail,
                            RequestType = requestType,
                            Status = "Pending",
                            CurrentStage = "Submitted",
                            RequestNumber = GenerateRequestNumber("OCITY"),
                            CreatedAt = DateTime.Now,
                            CreatedByUsername = username,
                            ManagerUsername = manager,
                            UserModificationDate = userModificationDate,
                            UpdateType = updateType,
                            UpdateDate = updateDate,
                            RoleModified = roleModified
                        });
                    }

                    success++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"Row {row.RowNumber()}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success,
                failed,
                errors
            });
        }

        // 🔵 دالة للتحقق من صحة RequestType
        //private bool IsValidRequestType(string requestType)
        //{
        //    var validTypes = new[] { "Create", "Modify", "Disable" };
        //    return validTypes.Contains(requestType, StringComparer.OrdinalIgnoreCase);
        //}
        [HttpGet("template")]
        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();

            // ================================================
            // 1) InvoiceQ Template
            // ================================================
            var invoiceSheet = workbook.AddWorksheet("InvoiceQ");

            invoiceSheet.Cell(1, 1).Value = "Name";
            invoiceSheet.Cell(1, 2).Value = "MobileNumber";
            invoiceSheet.Cell(1, 3).Value = "Email";
            invoiceSheet.Cell(1, 4).Value = "Department";
            invoiceSheet.Cell(1, 5).Value = "JobTitle";
            invoiceSheet.Cell(1, 6).Value = "Unit";
            invoiceSheet.Cell(1, 7).Value = "RequestDetails";

            invoiceSheet.Range("A1:G1").Style.Font.Bold = true;
            invoiceSheet.Columns().AdjustToContents();

            // Sample Row (optional)
            invoiceSheet.Cell(2, 1).Value = "Example: Mohamed Ali";
            invoiceSheet.Cell(2, 2).Value = "0551234567";
            invoiceSheet.Cell(2, 3).Value = "mohamed@example.com";
            invoiceSheet.Cell(2, 4).Value = "Finance";
            invoiceSheet.Cell(2, 5).Value = "Accountant";
            invoiceSheet.Cell(2, 6).Value = "Unit A";
            invoiceSheet.Cell(2, 7).Value = "Request access to InvoiceQ";

            // ================================================
            // 2) Ocity Template
            // ================================================
            var ocitySheet = workbook.AddWorksheet("Ocity");

            ocitySheet.Cell(1, 1).Value = "Name";
            ocitySheet.Cell(1, 2).Value = "MobileNumber";
            ocitySheet.Cell(1, 3).Value = "Email";
            ocitySheet.Cell(1, 4).Value = "Project";
            ocitySheet.Cell(1, 5).Value = "JobTitle";
            ocitySheet.Cell(1, 6).Value = "RequestDetails";

            ocitySheet.Range("A1:F1").Style.Font.Bold = true;
            ocitySheet.Columns().AdjustToContents();

            // Sample Row
            ocitySheet.Cell(2, 1).Value = "Example: Sarah Ahmed";
            ocitySheet.Cell(2, 2).Value = "0569876543";
            ocitySheet.Cell(2, 3).Value = "sarah@example.com";
            ocitySheet.Cell(2, 4).Value = "Project Falcon";
            ocitySheet.Cell(2, 5).Value = "Engineer";
            ocitySheet.Cell(2, 6).Value = "Request access to Ocity";

            // ================================================
            // 3) Instructions Sheet
            // ================================================
            var inst = workbook.AddWorksheet("Instructions");

            inst.Cell(1, 1).Value = "BULK UPLOAD INSTRUCTIONS";
            inst.Range("A1:D1").Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            inst.Cell(3, 1).Value = "1. Choose the correct sheet based on the system:";
            inst.Cell(4, 2).Value = "- Use the 'InvoiceQ' sheet for InvoiceQ requests.";
            inst.Cell(5, 2).Value = "- Use the 'Ocity' sheet for Ocity requests.";

            inst.Cell(7, 1).Value = "2. Fill in all required fields:";
            inst.Cell(8, 2).Value = "- Name (required)";
            inst.Cell(9, 2).Value = "- MobileNumber (required)";
            inst.Cell(10, 2).Value = "- Email (required)";
            inst.Cell(11, 2).Value = "- Department / Project (depending on system)";
            inst.Cell(12, 2).Value = "- RequestDetails must describe the required access";

            inst.Cell(14, 1).Value = "3. Do NOT modify the header names.";
            inst.Cell(16, 1).Value = "4. You may delete the sample row before uploading.";

            inst.Cell(18, 1).Value = "5. Supported file formats:";
            inst.Cell(19, 2).Value = "- .xlsx";

            inst.Cell(21, 1).Value = "6. Upload via the Bulk Upload page in the system.";

            inst.Columns().AdjustToContents();

            // Export file
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BulkUploadTemplate.xlsx"
            );
        }

        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var employeeId = User.FindFirst("employeeId")?.Value;

            if (string.IsNullOrWhiteSpace(employeeId))
                return Unauthorized("Missing employeeId");

            var all = await _requestService.GetAllForUser(employeeId);

            var activeStatuses = new[] { "Pending", "FirstApproved", "InProgress" };
            var closedStatuses = new[] { "Rejected", "FinalApproved" , "Implemented" }; // ✅ عدلنا FinalApproved بدل Approved

            var active = all.Where(r => activeStatuses.Contains(r.Status)).ToList();
            var closed = all.Where(r => closedStatuses.Contains(r.Status)).ToList();

            return Ok(new
            {
                Active = active,
                Closed = closed
            });
        }

        [HttpGet("details")]
        public async Task<IActionResult> GetRequestDetails(int id, string system)
        {
            var result = await _requestService.GetRequestDetails(id, system);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // ============================================
        // HELPER METHOD
        // ============================================
        
        private string GenerateRequestNumber(string prefix)
        {
            return $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        [HttpPost("normalize-mobile")]
        public string NormalizeSaudiMobile(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return mobile;

            string cleanMobile = Regex.Replace(mobile, @"[\s\-]+", "");

            // إذا كان الرقم 9 أرقام فقط (مفقود 0)
            if (cleanMobile.Length == 9 && cleanMobile.All(char.IsDigit))
            {
                return "05" + cleanMobile; // أضف 05 في البداية
            }
            // إذا كان الرقم 8 أرقام بعد 966 (مفقود 5)
            else if (cleanMobile.Length == 11 && cleanMobile.StartsWith("966") && cleanMobile.Substring(3).All(char.IsDigit))
            {
                return "9665" + cleanMobile.Substring(3);
            }
            // إذا كان الرقم دولي بدون 5
            else if (cleanMobile.Length == 12 && cleanMobile.StartsWith("+966") && cleanMobile.Substring(4).All(char.IsDigit))
            {
                return "+9665" + cleanMobile.Substring(4);
            }

            return cleanMobile;
        }

    }


}