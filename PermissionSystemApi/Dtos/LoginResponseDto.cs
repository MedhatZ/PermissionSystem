namespace PermissionSystemApi.Dtos
{
    public class LoginResponseDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Token { get; set; }
    }
}
