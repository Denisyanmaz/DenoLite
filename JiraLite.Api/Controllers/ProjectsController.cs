using JiraLite.Application.DTOs;
using JiraLite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JiraLite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔐 JWT required
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        // 🔹 Add a member to project (Owner only)
        [HttpPost("{projectId}/members")]
        public async Task<IActionResult> AddMember(Guid projectId, [FromBody] ProjectMemberDto dto)
        {
            var currentUserId = GetUserId();

            if (!await _projectService.IsOwnerAsync(projectId, currentUserId))
                return Forbid();

            try
            {
                var memberDto = await _projectService.AddMemberAsync(projectId, dto, currentUserId);
                return Ok(memberDto);
            }
            catch (JiraLite.Application.Exceptions.ConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // 🔹 Create a project
        [HttpPost]
        public async Task<IActionResult> Create(CreateProjectDto dto)
        {
            var result = await _projectService.CreateAsync(GetUserId(), dto);
            return Ok(result);
        }

        // 🔹 Get projects for current user
        [HttpGet]
        public async Task<IActionResult> MyProjects()
        {
            var projects = await _projectService.GetMyProjectsAsync(GetUserId());
            return Ok(projects);
        }

        // Helper to extract current user ID from JWT
        private Guid GetUserId()
        {
            return Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );
        }
    }
}
