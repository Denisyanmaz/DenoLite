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
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpPost("{projectId}/members")]
        public async Task<IActionResult> AddMember(Guid projectId, [FromBody] ProjectMemberDto dto)
        {
            var member = await _projectService.AddMemberAsync(
                projectId,
                dto,
                GetUserId()
            );

            return Ok(member);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
        {
            var project = await _projectService.CreateAsync(GetUserId(), dto);
            return Ok(project);
        }

        [HttpGet]
        public async Task<IActionResult> MyProjects()
        {
            var projects = await _projectService.GetMyProjectsAsync(GetUserId());
            return Ok(projects);
        }

        private Guid GetUserId()
        {
            return Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );
        }
    }
}
