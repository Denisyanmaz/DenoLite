using DenoLite.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;

namespace DenoLite.Web.Pages
{
    public class VerifyEmailModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<VerifyEmailModel> _logger;

        public VerifyEmailModel(IHttpClientFactory httpClientFactory, ILogger<VerifyEmailModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [BindProperty]
        public VerifyEmailInput Input { get; set; } = new();

        public string? Error { get; set; }
        public string? Success { get; set; }

        public void OnGet(string? email = null)
        {
            if (!string.IsNullOrWhiteSpace(email))
                Input.Email = email;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Verify email: validation failed, not calling API. Errors: {Errors}",
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return Page();
            }

            var client = _httpClientFactory.CreateClient("DenoLiteApi");
            _logger.LogInformation("Verify email: calling API POST /api/auth/verify-email for {Email}", Input.Email);

            // API expects: { email, code }
            var resp = await client.PostAsJsonAsync("/api/auth/verify-email", new
            {
                email = Input.Email,
                code = Input.Code
            });

            _logger.LogInformation("Verify email: API responded {StatusCode}", resp.StatusCode);

            if (!resp.IsSuccessStatusCode)
            {
                Error = await ApiErrorReader.ReadFriendlyMessageAsync(resp);
                return Page();
            }

            // Optional: redirect to login with email prefilled
            return RedirectToPage("/Login", new { email = Input.Email, verified = true });
        }

        public async Task<IActionResult> OnPostResendAsync()
        {
            // ✅ Resend doesn't need code. Remove Code validation errors if they exist.
            ModelState.Remove("Input.Code");
            Input.Code = ""; // optional: clear the textbox

            // Also clear any previous errors so they don't show under the field
            ModelState.Clear();
            // Validate email only for resend
            if (string.IsNullOrWhiteSpace(Input.Email))
            {
                Error = "Please enter your email to resend the code.";
                return Page();
            }

            var client = _httpClientFactory.CreateClient("DenoLiteApi");

            var resp = await client.PostAsJsonAsync("/api/auth/resend-verification", new
            {
                email = Input.Email
            });

            if (!resp.IsSuccessStatusCode)
            {
                Error = await ApiErrorReader.ReadFriendlyMessageAsync(resp);
                return Page();
            }

            Success = "A new verification code was sent. Please check your email.";
            return Page();
        }

        public class VerifyEmailInput
        {
            [Required, EmailAddress, StringLength(254)]
            public string Email { get; set; } = "";

            [Required, StringLength(6, MinimumLength = 6)]
            [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits.")]
            public string Code { get; set; } = "";
        }
    }
}
