using DenoLite.Application.DTOs.Auth;
using DenoLite.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;

namespace DenoLite.Web.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public RegisterModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [BindProperty]
        public RegisterInput Input { get; set; } = new();

        public string? Error { get; set; }
        public string ApiBaseUrl => _configuration["Api:BaseUrl"] ?? "https://localhost:7144";

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var client = _httpClientFactory.CreateClient("DenoLiteApi");

            // 1) Register
            var registerPayload = new RegisterUserDto
            {
                Email = Input.Email,
                Password = Input.Password
            };

            var registerResp = await client.PostAsJsonAsync("/api/auth/register", registerPayload);

            if (!registerResp.IsSuccessStatusCode)
            {
                Error = await ApiErrorReader.ReadFriendlyMessageAsync(registerResp);
                return Page();
            }
            // ✅ After register, go to verification page
            return RedirectToPage("/VerifyEmail", new { email = Input.Email });

        }

        public class RegisterInput
        {
            [Required, EmailAddress, StringLength(254)]
            public string Email { get; set; } = "";

            [Required, StringLength(100, MinimumLength = 6)]
            public string Password { get; set; } = "";

            [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = "";
        }
    }
}
