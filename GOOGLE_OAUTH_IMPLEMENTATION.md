# Google OAuth (Gmail Login/Register) Implementation Guide

## Overview
This guide documents the Google OAuth authentication implementation for both login and registration in the DenoLite application.

---

## Step 1: NuGet Packages

**DenoLite.Api.csproj:**
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.Google" Version="10.0.0" />
```

**DenoLite.Web.csproj:**
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.Google" Version="10.0.0" />
```

Run: `dotnet restore` after adding packages.

---

## Step 2: User Entity

**DenoLite.Domain/Entities/User.cs:**
```csharp
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? GoogleId { get; set; } // For OAuth users
    public string Role { get; set; } = "User"; // Admin, User
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;
}
```

**Create and apply migration:**
```bash
dotnet ef migrations add AddGoogleIdToUser --project DenoLite.Infrastructure --startup-project DenoLite.Api
dotnet ef database update --project DenoLite.Infrastructure --startup-project DenoLite.Api
```

---

## Step 3: API – Authentication (Program.cs)

Authentication uses JWT as default, Cookies for the OAuth flow, and Google with Cookies as sign-in scheme:

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5); // Short-lived, for OAuth flow only
})
.AddJwtBearer(options => { /* ... */ })
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Google:ClientId"] ?? throw new InvalidOperationException("Google:ClientId is required");
    options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? throw new InvalidOperationException("Google:ClientSecret is required");
    options.CallbackPath = "/api/auth/google-callback";
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    options.Scope.Add("email");
    options.Scope.Add("profile");
    options.SaveTokens = true;
});
```

**CORS** (API and Web on different ports):
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy.WithOrigins("https://localhost:7002", "http://localhost:5142")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// In pipeline (after UseHttpsRedirection):
app.UseRouting();
app.UseCors("AllowWebApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

**Configuration (e.g. .env):**
```env
Google__ClientId=your-client-id
Google__ClientSecret=your-client-secret
WebApp__BaseUrl=https://localhost:7002
```

---

## Step 4: IAuthService

**DenoLite.Application/Interfaces/IAuthService.cs:**
```csharp
Task<AuthResponseDto> AuthenticateWithGoogleAsync(string googleId, string email);
```

---

## Step 5: AuthService – Google authentication

**DenoLite.Infrastructure/Services/AuthService.cs:**
```csharp
public async Task<AuthResponseDto> AuthenticateWithGoogleAsync(string googleId, string email)
{
    email = email.Trim().ToLowerInvariant();

    var user = await _db.Users.FirstOrDefaultAsync(u =>
        u.GoogleId == googleId || u.Email.ToLower() == email);

    if (user == null)
    {
        user = new User
        {
            Email = email,
            GoogleId = googleId,
            PasswordHash = string.Empty,
            Role = "User",
            IsActive = true,
            IsEmailVerified = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }
    else
    {
        if (string.IsNullOrEmpty(user.GoogleId))
        {
            user.GoogleId = googleId;
            await _db.SaveChangesAsync();
        }

        if (!user.IsEmailVerified)
        {
            user.IsEmailVerified = true;
            await _db.SaveChangesAsync();
        }

        if (!user.IsActive)
            throw new ForbiddenException("User is inactive.");
    }

    return new AuthResponseDto
    {
        Email = user.Email,
        Role = user.Role,
        Token = GenerateJwtToken(user)
    };
}
```

---

## Step 6: AuthController – Google endpoints

Flow: **google-login** → Google → **google-callback** (handled by middleware) → redirect to **google-callback-handler** (controller).

**DenoLite.Api/Controllers/AuthController.cs:**

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;

[HttpGet("google-login")]
public IActionResult GoogleLogin()
{
    var redirectUrl = Url.Action(nameof(GoogleCallbackHandler), "Auth", null, Request.Scheme);
    var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
    return Challenge(properties, "Google");
}

[HttpGet("google-callback-handler")]
public async Task<IActionResult> GoogleCallbackHandler()
{
    try
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded || result.Principal == null)
        {
            var webAppUrl = GetWebAppUrl();
            return Redirect($"{webAppUrl}/GoogleCallback?error=auth_failed");
        }

        var claims = result.Principal.Claims.ToList();
        var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
        {
            var webAppUrl = GetWebAppUrl();
            return Redirect($"{webAppUrl}/GoogleCallback?error=missing_info");
        }

        var authResult = await _authService.AuthenticateWithGoogleAsync(googleId, email);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var baseUrl = GetWebAppUrl();
        var token = Uri.EscapeDataString(authResult.Token);
        return Redirect($"{baseUrl}/GoogleCallback?token={token}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in Google callback: {Message}", ex.Message);
        var webAppUrl = GetWebAppUrl();
        return Redirect($"{webAppUrl}/GoogleCallback?error=server_error");
    }
}

private string GetWebAppUrl()
{
    var configuredUrl = _configuration["WebApp:BaseUrl"];
    if (!string.IsNullOrEmpty(configuredUrl))
        return configuredUrl;

    var scheme = Request.Scheme;
    var host = Request.Host.Host;
    var port = Request.Host.Port;

    if (port.HasValue && port.Value != 5001 && port.Value != 5000)
        return $"{scheme}://{host}:5001";

    return $"{scheme}://{host}" + (port.HasValue ? $":{port.Value}" : "");
}
```

---

## Step 7: Web – Login and Register (Google button)

**Login.cshtml / Register.cshtml** – link must target the **API** base URL (e.g. API on 7144, Web on 7002):

```html
<div class="mb-3">
    <a href="@Model.ApiBaseUrl/api/auth/google-login" class="btn btn-outline-danger w-100 d-flex align-items-center justify-content-center" style="gap: 8px;">
        <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" fill="currentColor" viewBox="0 0 16 16">
            <path d="M15.545 6.558a9.42 9.42 0 0 1 .139 1.626c0 2.434-.87 4.492-2.384 5.885h.002C11.978 15.292 10.158 16 8 16A8 8 0 1 1 8 0a7.689 7.689 0 0 1 5.352 2.082l-2.284 2.284A4.347 4.347 0 0 0 8 3.166c-2.087 0-3.86 1.408-4.492 3.304a4.792 4.792 0 0 0 0 3.063h.003c.635 1.893 2.405 3.301 4.492 3.301 1.078 0 2.004-.276 2.722-.764h-.003a3.702 3.702 0 0 0 1.599-2.431H8v-3.08h7.545z"/>
        </svg>
        Sign in with Google
    </a>
</div>
```

**Login.cshtml.cs / Register.cshtml.cs** – expose API base URL from config:

```csharp
private readonly IConfiguration _configuration;

public string ApiBaseUrl => _configuration["Api:BaseUrl"] ?? "https://localhost:7144";
```

**Web appsettings (e.g. appsettings.Development.json):**
```json
{
  "Api": {
    "BaseUrl": "https://localhost:7144"
  }
}
```

---

## Step 8: Web – GoogleCallback page

After Google sign-in, the API redirects to the Web app with `?token=...` or `?error=...`. The **GoogleCallback** page stores the JWT and cookie auth, then redirects to the app.

**GoogleCallback.cshtml** – show success or error and redirect on success.

**GoogleCallback.cshtml.cs** – in `OnGetAsync(string? token, string? error)`:
- If `error`: set `Error` and show “Go to Login”.
- If `token`: decode JWT (e.g. with `JwtSecurityTokenHandler`), set `DenoLite_jwt` and `DenoLite_email` cookies, call `HttpContext.SignInAsync("Cookies", principal, ...)`, then redirect to `/Projects/Index` (e.g. after 1 second via script).

---

## Step 9: Google Cloud Console

1. [Google Cloud Console](https://console.cloud.google.com/) → your project.
2. Enable Google Identity (or relevant) API.
3. **Credentials** → Create **OAuth 2.0 Client ID** (Web application).
4. **Authorized redirect URIs** must match the **API** callback URL, e.g.:
   - `https://localhost:7144/api/auth/google-callback`
   - Production: `https://your-api-domain/api/auth/google-callback`
5. Copy Client ID and Client Secret into config (`Google__ClientId`, `Google__ClientSecret`).

---

## Flow summary

1. User clicks “Sign in with Google” on Web (e.g. `https://localhost:7002/Login`).
2. Browser goes to **API** `https://localhost:7144/api/auth/google-login`.
3. API issues Challenge → redirect to Google.
4. User signs in with Google → Google redirects to **API** `https://localhost:7144/api/auth/google-callback`.
5. Middleware handles callback, signs user into **Cookies**, then redirects to **API** `https://localhost:7144/api/auth/google-callback-handler`.
6. **GoogleCallbackHandler** reads Cookie auth, gets `googleId` and `email`, calls `AuthenticateWithGoogleAsync(googleId, email)`, then redirects to **Web** `https://localhost:7002/GoogleCallback?token=...`.
7. **GoogleCallback** page receives token, sets JWT and cookie auth, redirects to `/Projects/Index`.

---

## Google-only users (no password)

- **Login (email/password):** If user has no password (Google-only), return a clear error, e.g. “This account uses Google sign-in. Please sign in with Google instead.”
- **Change password:** If user has no password, allow setting a new password without verifying an old one (so they can later use email/password too).

---

## Testing checklist

- [ ] “Sign in with Google” on Login/Register goes to API then Google.
- [ ] After Google consent, redirect to API callback then to Web `/GoogleCallback?token=...`.
- [ ] New users are created with `GoogleId`, `IsEmailVerified = true`, `IsActive = true`, empty `PasswordHash`.
- [ ] Existing users get `GoogleId` and `IsEmailVerified` updated when they sign in with Google.
- [ ] JWT is stored in cookie and user can access protected routes.
- [ ] Google-only users cannot use email/password login and see the correct message.
- [ ] Google-only users can set a password from Account/Change password.

---

## Security note

Google sign-in is validated by ASP.NET Core’s Google authentication middleware. Do not skip middleware or accept a raw token from the client without server-side validation.
