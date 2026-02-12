using JiraLite.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace JiraLite.Web.Pages.Projects
{
    [IgnoreAntiforgeryToken] // because we’re calling it via fetch() JSON
    public class UpdateTaskStatusModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public UpdateTaskStatusModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public sealed class UpdateTaskStatusDto
        {
            public Guid TaskId { get; set; }

            [JsonConverter(typeof(JsonStringEnumConverter))]
            public JiraTaskStatus Status { get; set; }

            public Guid ProjectId { get; set; } // optional, but handy for future checks
        }

        public async Task<IActionResult> OnPostAsync([FromBody] UpdateTaskStatusDto payload)
        {
            if (payload == null)
                return BadRequest("Payload was null. Make sure Content-Type is application/json.");

            if (payload.TaskId == Guid.Empty)
                return BadRequest("TaskId is required.");

            var client = _httpClientFactory.CreateClient("JiraLiteApi");

            // Call API patch endpoint (you must have this in API)
            var resp = await client.PatchAsJsonAsync(
                $"/api/tasks/{payload.TaskId}/status",
                new { status = payload.Status }
            );

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                return StatusCode(401);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return StatusCode((int)resp.StatusCode, body);
            }

            return new OkResult();
        }
    }
}
