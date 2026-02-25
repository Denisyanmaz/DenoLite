using DenoLite.Application.DTOs.Auth;
using DenoLite.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;

namespace DenoLite.Web.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ForgotPasswordModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [BindProperty]
        public ForgotPasswordInput Input { get; set; } = new();

        [BindProperty]
        public ResetPasswordInput ResetInput { get; set; } = new();

        public string? Success { get; set; }
        public string? Error { get; set; }
        public bool CodeSent { get; set; } = false;

        public void OnGet(bool codeSent = false, string? email = null)
        {
            CodeSent = codeSent;
            if (!string.IsNullOrWhiteSpace(email))
                Input.Email = email;
            
            if (codeSent)
                Success = "A verification code has been sent to your email. Please check your inbox.";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Clear validation errors for ResetInput fields (they're not part of step 1)
            ModelState.Remove("ResetInput.Code");
            ModelState.Remove("ResetInput.NewPassword");
            ModelState.Remove("ResetInput.ConfirmPassword");
            
            if (!ModelState.IsValid)
            {
                Error = "Please enter a valid email address.";
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("DenoLiteApi");

                var payload = new ForgotPasswordDto
                {
                    Email = Input.Email
                };

                var resp = await client.PostAsJsonAsync("/api/auth/forgot-password", payload);

                if (!resp.IsSuccessStatusCode)
                {
                    Error = await ApiErrorReader.ReadFriendlyMessageAsync(resp);
                    return Page();
                }

                // Redirect to same page with codeSent=true to show the code entry form
                return RedirectToPage("/ForgotPassword", new { codeSent = true, email = Input.Email });
            }
            catch (HttpRequestException ex)
            {
                Error = $"Unable to connect to the server. Please make sure the API is running. Error: {ex.Message}";
                return Page();
            }
            catch (Exception ex)
            {
                Error = $"An unexpected error occurred: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostResetAsync()
        {
            // Manual validation
            if (string.IsNullOrWhiteSpace(ResetInput.Code))
            {
                Error = "Verification code is required.";
                CodeSent = true;
                return Page();
            }
            
            if (ResetInput.Code.Length != 6 || !ResetInput.Code.All(char.IsDigit))
            {
                Error = "Code must be exactly 6 digits.";
                CodeSent = true;
                return Page();
            }
            
            if (string.IsNullOrWhiteSpace(ResetInput.NewPassword))
            {
                Error = "New password is required.";
                CodeSent = true;
                return Page();
            }
            
            if (ResetInput.NewPassword.Length < 6)
            {
                Error = "Password must be at least 6 characters.";
                CodeSent = true;
                return Page();
            }
            
            if (string.IsNullOrWhiteSpace(ResetInput.ConfirmPassword))
            {
                Error = "Please confirm your password.";
                CodeSent = true;
                return Page();
            }

            if (ResetInput.NewPassword != ResetInput.ConfirmPassword)
            {
                Error = "Passwords do not match.";
                CodeSent = true;
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("DenoLiteApi");

                var payload = new ResetPasswordDto
                {
                    Email = Input.Email,
                    Code = ResetInput.Code,
                    NewPassword = ResetInput.NewPassword
                };

                var resp = await client.PostAsJsonAsync("/api/auth/reset-password", payload);

                if (!resp.IsSuccessStatusCode)
                {
                    Error = await ApiErrorReader.ReadFriendlyMessageAsync(resp);
                    CodeSent = true;
                    return Page();
                }

                // Redirect to login with success message
                TempData["SuccessMessage"] = "Password reset successfully! You can now login with your new password.";
                return RedirectToPage("/Login", new { email = Input.Email });
            }
            catch (HttpRequestException ex)
            {
                Error = $"Unable to connect to the server. Please make sure the API is running. Error: {ex.Message}";
                CodeSent = true;
                return Page();
            }
            catch (Exception ex)
            {
                Error = $"An unexpected error occurred: {ex.Message}";
                CodeSent = true;
                return Page();
            }
        }

        public class ForgotPasswordInput
        {
            [Required, EmailAddress, StringLength(254)]
            public string Email { get; set; } = "";
        }

        public class ResetPasswordInput
        {
            public string Code { get; set; } = "";
            public string NewPassword { get; set; } = "";
            public string ConfirmPassword { get; set; } = "";
        }
    }
}
