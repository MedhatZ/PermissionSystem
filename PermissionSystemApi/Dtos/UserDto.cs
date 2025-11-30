namespace PermissionSystemApi.Dtos
{
    public class UserDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }

    public class LoginFromAdDto
    {
        public AdUserDto User { get; set; }
        public AdManagerDto DirectManager { get; set; }
    }

    public class AdUserDto
    {
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string Department { get; set; }
        public string EmployeeID { get; set; }
        public bool IsGM { get; set; }
    }

    public class AdManagerDto
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string Department { get; set; }
        public string EmployeeID { get; set; }
    }

}

