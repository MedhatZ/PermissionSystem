using ClosedXML.Excel;
using PermissionSystemApi.Dtos;
using PermissionSystemApi.Models;

namespace PermissionSystemApi.Services
{
    public class ExcelService
    {
        // ============================================
        // EXPORT PERMISSION RECORDS
        // ============================================

        public byte[] ExportPermissions(List<PermissionRecord> records)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Permissions");

            ws.Cell(1, 1).Value = "RequestNumber";
            ws.Cell(1, 2).Value = "UserId";
            ws.Cell(1, 3).Value = "PermissionDetails";
            ws.Cell(1, 4).Value = "Status";
            ws.Cell(1, 5).Value = "CurrentStage";

            int row = 2;

            foreach (var r in records)
            {
                ws.Cell(row, 1).Value = r.RequestNumber;
                ws.Cell(row, 2).Value = r.UserId;
                ws.Cell(row, 3).Value = r.PermissionDetails;
                ws.Cell(row, 4).Value = r.Status;
                ws.Cell(row, 5).Value = r.CurrentStage;
                row++;
            }

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }

        // ============================================
        // IMPORT BULK REQUESTS
        // ============================================

        public async Task<int> ImportRequests(ExcelUploadDto dto, RequestService requestService)
        {
            int imported = 0;

            using var stream = dto.File.OpenReadStream();
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheet(1);

            int row = 2; // skip header

            while (!ws.Row(row).IsEmpty())
            {
                string name = ws.Cell(row, 1).GetString();
                string mobile = ws.Cell(row, 2).GetString();
                string email = ws.Cell(row, 3).GetString();
                string field4 = ws.Cell(row, 4).GetString();
                string job = ws.Cell(row, 5).GetString();

                if (dto.SystemName == "InvoiceQ")
                {
                    await requestService.CreateInvoiceQRequest(new CreateInvoiceQRequestDto
                    {
                        Name = name,
                        MobileNumber = mobile,
                        Email = email,
                        Department = field4,
                        JobTitle = job,
                        Unit = ws.Cell(row, 6).GetString()
                    });
                }
                else if (dto.SystemName == "Ocity")
                {
                    await requestService.CreateOcityRequest(new CreateOcityRequestDto
                    {
                        Name = name,
                        MobileNumber = mobile,
                        Email = email,
                        Project = field4,
                        JobTitle = job
                    });
                }

                imported++;
                row++;
            }

            return imported;
        }
    }
}