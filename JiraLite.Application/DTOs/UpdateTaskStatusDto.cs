using JiraLite.Domain.Enums;

namespace JiraLite.Application.DTOs
{
    public class UpdateTaskStatusDto
    {
        public JiraTaskStatus Status { get; set; }
    }

}
