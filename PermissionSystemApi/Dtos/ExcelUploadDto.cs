using Microsoft.AspNetCore.Http;

namespace PermissionSystemApi.Dtos
{
    // ============================================
    // DTOs
    // ============================================

    public class ExcelUploadDto
    {
        public IFormFile File { get; set; }
        public string SystemName { get; set; } // "InvoiceQ" or "Ocity"
    }
}
