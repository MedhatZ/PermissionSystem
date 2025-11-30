// ============================================
// DTOs
// ============================================

public class LoginResponseDto
{
    public string Token { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; }
}

public class LoginVm
{
    public string Username { get; set; }
    public string Password { get; set; }
}