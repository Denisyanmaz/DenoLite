using System.ComponentModel.DataAnnotations;

namespace DenoLite.Application.DTOs.Auth
{
    public class ChangePasswordDto
    {
        /// <summary>Current password. Leave empty if you signed in with Google and have not set a password yet.</summary>
        [StringLength(100)]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
