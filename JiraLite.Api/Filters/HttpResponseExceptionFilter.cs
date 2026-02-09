using JiraLite.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JiraLite.Api.Filters
{
    public class HttpResponseExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            switch (context.Exception)
            {
                case ForbiddenException ex:
                    context.Result = new ObjectResult(new { message = ex.Message })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    context.ExceptionHandled = true;
                    break;

                case ConflictException ex:
                    context.Result = new ObjectResult(new { message = ex.Message })
                    {
                        StatusCode = StatusCodes.Status409Conflict
                    };
                    context.ExceptionHandled = true;
                    break;

                case KeyNotFoundException ex:
                    context.Result = new NotFoundObjectResult(new { message = ex.Message });
                    context.ExceptionHandled = true;
                    break;
            }
        }
    }
}
