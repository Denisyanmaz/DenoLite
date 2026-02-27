namespace DenoLite.Application.DTOs.ProjectMember
{
    public class AddMemberByEmailDto
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Member";
    }
}