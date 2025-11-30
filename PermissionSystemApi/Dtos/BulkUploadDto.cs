namespace PermissionSystemApi.Dtos
{
    public class BulkUploadDto
    {
        public IFormFile File { get; set; }
        public string systemType { get; set; }
    }

}
