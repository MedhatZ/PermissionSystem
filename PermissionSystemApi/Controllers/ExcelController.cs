using Microsoft.AspNetCore.Mvc;
using PermissionSystemApi.Services;
using PermissionSystemApi.Dtos;

namespace PermissionSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExcelController : ControllerBase
    {
        private readonly ExcelService _excelService;
        private readonly ReviewService _reviewService;
        private readonly RequestService _requestService;

        public ExcelController(
            ExcelService excelService,
            ReviewService reviewService,
            RequestService requestService)
        {
            _excelService = excelService;
            _reviewService = reviewService;
            _requestService = requestService;
        }

        // ============================================
        // EXPORT PERMISSION RECORDS AS EXCEL
        // ============================================

        [HttpGet("export-permissions")]
        public async Task<IActionResult> ExportPermissions()
        {
            var records = await _reviewService.GetPermissions();
            var fileBytes = _excelService.ExportPermissions(records);

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "permissions.xlsx"
            );
        }

        // ============================================
        // IMPORT BULK REQUESTS FROM EXCEL
        // ============================================

        [HttpPost("import-requests")]
        public async Task<IActionResult> ImportRequests([FromForm] ExcelUploadDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("File not found");

            try
            {
                var importedCount = await _excelService.ImportRequests(dto, _requestService);
                return Ok(new
                {
                    message = "Requests imported successfully",
                    imported = importedCount
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error importing file",
                    error = ex.Message
                });
            }
        }
    }

   
}