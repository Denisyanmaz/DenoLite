using DenoLite.Application.DTOs.Auth;
using DenoLite.Web.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;

namespace DenoLite.Web.Pages
{
    public class GoogleCallbackModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GoogleCallbackModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public string? Error { get; set; }

        public async Task<IActionResult> OnGetAsync(string? token = null, string? error = null)
        {
            if (!string.IsNullOrEmpty(error))
            {
                Error = error switch
                {
                    "auth_failed" => "Google authentication failed.",
                    "missing_info" => "Failed to retrieve Google account information.",
                    _ => "Google authentication was cancelled or failed."
                };
                return Page();
            }

            if (string.IsNullOrEmpty(token))
            {
                Error = "No authentication token received.";
                return Page();
            }

            // Decode the token
            var decodedToken = Uri.UnescapeDataString(token);
            
            // Get user info from token (we'll need to decode JWT or call API)
            // For now, let's call an API endpoint to get user info, or decode JWT
            // Simplest: store token and redirect, let the app handle auth on next request
            // But we need email for the cookie, so let's decode JWT claims
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            if (!handler.CanReadToken(decodedToken))
            {
                Error = "Invalid token format.";
                return Page();
            }

            var jwtToken = handler.ReadJwtToken(decodedToken);
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == System.Security.Claims.ClaimTypes.Email)?.Value ?? "";

            var auth = new AuthResponseDto
            {
                Token = decodedToken,
                Email = email,
                Role = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value ?? "User"
            };

            // Store JWT in HttpOnly cookie (same as regular login)
            Response.Cookies.Append(
                "DenoLite_jwt",
                auth.Token,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddHours(4)
                });

            Response.Cookies.Append("DenoLite_email", auth.Email, new CookieOptions
            {
                HttpOnly = false,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(4)
            });

            // Sign in using ASP.NET Cookie auth
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, auth.Email ?? "")
            };

            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Cookies");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal, new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4)
            });

            return Page(); // Show success message, JS will redirect
        }
    }
}
