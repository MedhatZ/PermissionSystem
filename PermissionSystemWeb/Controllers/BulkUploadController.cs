using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using PermissionSystemWeb.Models.ViewModels;
using System.IO;
using System.Net.Http.Headers;

namespace PermissionSystemWeb.Controllers
{

   
    public class BulkUploadController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ApiSettings _settings;

        public BulkUploadController(IHttpClientFactory clientFactory, ApiSettings settings)
        {
            _clientFactory = clientFactory;
            _settings = settings;
        }

        [HttpGet]
        public IActionResult BulkUpload()
        {
            return View(new BulkUploadVm());
        }

        [HttpPost]
        public async Task<IActionResult> Upload(BulkUploadVm vm)
        {
            if (vm.ExcelFile == null || vm.ExcelFile.Length == 0)
            {
                TempData["Error"] = "Please upload a valid Excel file.";
                return RedirectToAction("BulkUpload");
            }

            if (string.IsNullOrEmpty(vm.SystemType))
            {
                TempData["Error"] = "Please select system type.";
                return RedirectToAction("BulkUpload");
            }

            var token = HttpContext.Session.GetString("token");
            var client = _clientFactory.CreateClient("api");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            using var ms = new MemoryStream();
            await vm.ExcelFile.CopyToAsync(ms);

            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(ms.ToArray()), "file", vm.ExcelFile.FileName);
            content.Add(new StringContent(vm.SystemType), "systemType"); // غير systemName ل systemType

            var response = await client.PostAsync("api/requests/bulk-upload", content);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Upload failed. Please check the file.";
                return RedirectToAction("BulkUpload");
            }

            var result = await response.Content.ReadFromJsonAsync<BulkResultVm>();

            TempData["Success"] = result.Success;
            TempData["Failed"] = result.Failed;
            TempData["Errors"] = result.Errors;

            return RedirectToAction("BulkUpload");
        }
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            using var wb = new XLWorkbook();

            // Tab 1: InvoiceQ Template
            var invoiceQSheet = wb.Worksheets.Add("InvoiceQ");
            CreateInvoiceQTemplate(invoiceQSheet);

            // Tab 2: Ocity Template
            var ocitySheet = wb.Worksheets.Add("Ocity");
            CreateOcityTemplate(ocitySheet);

            // Tab 3: Instructions
            var instructionsSheet = wb.Worksheets.Add("Instructions");
            CreateInstructionsTemplate(instructionsSheet);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;
            var fileName = "bulk_upload_template.xlsx";
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private void CreateInvoiceQTemplate(IXLWorksheet ws)
        {
            // Header row for InvoiceQ
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "MobileNumber";
            ws.Cell(1, 3).Value = "Email";
            ws.Cell(1, 4).Value = "Department";
            ws.Cell(1, 5).Value = "JobTitle";
            ws.Cell(1, 6).Value = "Unit";
            ws.Cell(1, 7).Value = "TaskDetail";
            ws.Cell(1, 8).Value = "RequestType";
            ws.Cell(1, 9).Value = "User Modification Date";
            ws.Cell(1, 10).Value = "Update Type";
            ws.Cell(1, 11).Value = "Update Date";
            ws.Cell(1, 12).Value = "Role Modified";

            // Add a sample row
            ws.Cell(2, 1).Value = "Example: Ahmed Ali";
            ws.Cell(2, 2).Value = "05XXXXXXXX";
            ws.Cell(2, 3).Value = "user@example.com";
            ws.Cell(2, 4).Value = "Finance";
            ws.Cell(2, 5).Value = "Accountant";
            ws.Cell(2, 6).Value = "Unit A";
            ws.Cell(2, 7).Value = "Request for new employee permissions";
            ws.Cell(2, 8).Value = "Create";
            ws.Cell(2, 9).Value = DateTime.Now.ToString("yyyy-MM-dd");
            ws.Cell(2, 10).Value = "Permission Update";
            ws.Cell(2, 11).Value = DateTime.Now.ToString("yyyy-MM-dd");
            ws.Cell(2, 12).Value = "Accountant Role";

            // Format header
            var headerRange = ws.Range(1, 1, 1, 12);
            headerRange.Style.Font.Bold = true;

            // Create dropdown lists
            var requestTypeRange = ws.Range(2, 8, 1000, 8);
            requestTypeRange.CreateDataValidation().List("\"Create,Modify,Disable\"");

            var updateTypeRange = ws.Range(2, 10, 1000, 10);
            updateTypeRange.CreateDataValidation().List("\"Permission Update,Role Change,Access Modification\"");

            ws.Columns().AdjustToContents();
        }

        private void CreateOcityTemplate(IXLWorksheet ws)
        {
            // Header row for Ocity
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "MobileNumber";
            ws.Cell(1, 3).Value = "Email";
            ws.Cell(1, 4).Value = "JobTitle";
            ws.Cell(1, 5).Value = "Project";
            ws.Cell(1, 6).Value = "TaskDetail";
            ws.Cell(1, 7).Value = "RequestType";
            ws.Cell(1, 8).Value = "User Modification Date";
            ws.Cell(1, 9).Value = "Update Type";
            ws.Cell(1, 10).Value = "Update Date";
            ws.Cell(1, 11).Value = "Role Modified";

            // Add a sample row
            ws.Cell(2, 1).Value = "Example: Mohamed Ahmed";
            ws.Cell(2, 2).Value = "05XXXXXXXX";
            ws.Cell(2, 3).Value = "user@example.com";
            ws.Cell(2, 4).Value = "Engineer";
            ws.Cell(2, 5).Value = "Project X";
            ws.Cell(2, 6).Value = "Request for system access";
            ws.Cell(2, 7).Value = "Create";
            ws.Cell(2, 8).Value = DateTime.Now.ToString("yyyy-MM-dd");
            ws.Cell(2, 9).Value = "Permission Update";
            ws.Cell(2, 10).Value = DateTime.Now.ToString("yyyy-MM-dd");
            ws.Cell(2, 11).Value = "Project Manager Role";

            // Format header
            var headerRange = ws.Range(1, 1, 1, 11);
            headerRange.Style.Font.Bold = true;

            // Create dropdown lists
            var requestTypeRange = ws.Range(2, 7, 1000, 7);
            requestTypeRange.CreateDataValidation().List("\"Create,Modify,Disable\"");

            var updateTypeRange = ws.Range(2, 9, 1000, 9);
            updateTypeRange.CreateDataValidation().List("\"Permission Update,Role Change,Access Modification\"");

            ws.Columns().AdjustToContents();
        }

        private void CreateInstructionsTemplate(IXLWorksheet ws)
        {
            // عنوان رئيسي
            ws.Cell(1, 1).Value = "Bulk Upload Instructions";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;

            // تعليمات InvoiceQ
            ws.Cell(3, 1).Value = "InvoiceQ Instructions:";
            ws.Cell(3, 1).Style.Font.Bold = true;
            ws.Cell(4, 1).Value = "• RequestType must be: Create, Modify, or Disable";
            ws.Cell(5, 1).Value = "• Department, JobTitle, and Unit are required";
            ws.Cell(6, 1).Value = "• Fill all required fields";

            // تعليمات Ocity
            ws.Cell(8, 1).Value = "Ocity Instructions:";
            ws.Cell(8, 1).Style.Font.Bold = true;
            ws.Cell(9, 1).Value = "• RequestType must be: Create, Modify, or Disable";
            ws.Cell(10, 1).Value = "• Project is required";
            ws.Cell(11, 1).Value = "• Fill all required fields";

            // تعليمات عامة
            ws.Cell(13, 1).Value = "General Instructions:";
            ws.Cell(13, 1).Style.Font.Bold = true;
            ws.Cell(14, 1).Value = "• Download the template and fill the correct tab";
            ws.Cell(15, 1).Value = "• Select the system type when uploading";
            ws.Cell(16, 1).Value = "• Maximum file size is 5MB";

            ws.Columns().AdjustToContents();
        }

     
    }
}
