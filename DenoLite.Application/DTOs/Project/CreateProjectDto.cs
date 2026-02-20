using System.ComponentModel.DataAnnotations;

namespace DenoLite.Application.DTOs.Project
{
    public class CreateProjectDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [StringLength(400)]
        public string? Description { get; set; }
    }
}
