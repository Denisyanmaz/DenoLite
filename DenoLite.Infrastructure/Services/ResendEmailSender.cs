using DenoLite.Application.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace DenoLite.Infrastructure.Services
{
    /// <summary>
    /// Sends email via Resend HTTP API. Use when SMTP is blocked (e.g. on Render).
    /// Set Resend__ApiKey and Email__FromEmail / Email__FromName. If Resend__ApiKey is set, the API uses this sender instead of SMTP.
    /// </summary>
    public class ResendEmailSender : IEmailSender
    {
        private const string ResendApiUrl = "https://api.resend.com/emails";
        private readonly HttpClient _httpClient;
        private readonly EmailSettings _emailSettings;

        public ResendEmailSender(HttpClient httpClient, IOptions<EmailSettings> emailOptions)
        {
            _httpClient = httpClient;
            _emailSettings = emailOptions.Value;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var from = string.IsNullOrWhiteSpace(_emailSettings.FromName)
                ? _emailSettings.FromEmail
                : $"{_emailSettings.FromName} <{_emailSettings.FromEmail}>";

            var payload = new
            {
                from,
                to = new[] { toEmail },
                subject,
                html = htmlBody
            };

            var response = await _httpClient.PostAsJsonAsync(ResendApiUrl, payload);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Resend API error {(int)response.StatusCode}: {response.ReasonPhrase}. {body}");
            }
        }
    }
}
