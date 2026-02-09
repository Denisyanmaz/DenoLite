using JiraLite.Application.DTOs;
using JiraLite.Application.Exceptions;
using JiraLite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JiraLite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        // 🔹 Create task (only project members)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TaskItemDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var task = await _taskService.CreateTaskAsync(dto, userId);
                return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
        }

        // 🔹 Get task by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var task = await _taskService.GetTaskByIdAsync(id, userId);

                if (task == null)
                    return NotFound();

                return Ok(task);
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
        }

        // 🔹 Get tasks by project
        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetByProject(Guid projectId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tasks = await _taskService.GetTasksByProjectAsync(projectId, userId);
                return Ok(tasks);
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
        }

        // 🔹 Update task
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] TaskItemDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var task = await _taskService.UpdateTaskAsync(id, dto, userId);
                return Ok(task);
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
        }

        // 🔹 Delete task
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _taskService.DeleteTaskAsync(id, userId);
                return NoContent();
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
        }

        // 🔹 SAFE helper: extract Guid user id
        private Guid GetCurrentUserId()
        {
            // Always prefer our explicit "id" claim
            var idClaim = User.FindFirstValue("id");

            // Fallback only if needed
            if (string.IsNullOrWhiteSpace(idClaim))
                idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(idClaim))
                throw new UnauthorizedAccessException("User ID claim missing");

            if (!Guid.TryParse(idClaim, out var userId))
                throw new UnauthorizedAccessException($"Invalid user ID claim: '{idClaim}'");

            return userId;
        }
    }
}
