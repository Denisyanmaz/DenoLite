using System.ComponentModel.DataAnnotations;

namespace JiraLite.Application.DTOs
{
    public class ProjectMemberDto
    {
        [Required]
        public Guid UserId { get; set; }   // invite user id

        [Required]
        [RegularExpression("^(Owner|Member)$", ErrorMessage = "Role must be 'Owner' or 'Member'.")]
        public string Role { get; set; } = "Member";
    }
}
