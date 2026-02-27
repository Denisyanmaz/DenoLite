using System.ComponentModel.DataAnnotations;

namespace DenoLite.Application.DTOs.ProjectMember
{
    public class InviteProjectMemberDto
    {
        [Required, EmailAddress, MaxLength(50)]
        public string Email { get; set; } = string.Empty;
    }
}