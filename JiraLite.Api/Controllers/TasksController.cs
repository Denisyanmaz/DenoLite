using JiraLite.Application.DTOs;
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TaskItemDto dto)
        {
            var task = await _taskService.CreateTaskAsync(dto, GetCurrentUserId());
            return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var task = await _taskService.GetTaskByIdAsync(id, GetCurrentUserId());
            return task == null ? NotFound() : Ok(task);
        }

        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetByProject(Guid projectId)
        {
            var tasks = await _taskService.GetTasksByProjectAsync(projectId, GetCurrentUserId());
            return Ok(tasks);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] TaskItemDto dto)
        {
            var task = await _taskService.UpdateTaskAsync(id, dto, GetCurrentUserId());
            return Ok(task);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _taskService.DeleteTaskAsync(id, GetCurrentUserId());
            return NoContent();
        }

        private Guid GetCurrentUserId()
        {
            var idClaim =
                User.FindFirstValue("id")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(idClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user id");

            return userId;
        }
    }
}
