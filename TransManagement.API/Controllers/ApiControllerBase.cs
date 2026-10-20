using Microsoft.AspNetCore.Mvc;
using TransManagement.Application.Common.Models;

namespace TransManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ErrorResponse(Error error)
    {
        var statusCode = error.Code switch
        {
            "Transport.NotFound" => StatusCodes.Status404NotFound,
            "Transport.Conflict" => StatusCodes.Status409Conflict,
            "Transport.CustomerUnavailable" => StatusCodes.Status400BadRequest,
            "Chat.NotFound" => StatusCodes.Status404NotFound,
            "Chat.Closed" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        var details = new ProblemDetails
        {
            Status = statusCode,
            Title = "Operation failed",
            Detail = error.Description,
            Instance = HttpContext.Request.Path
        };
        details.Extensions["code"] = error.Code;
        details.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(statusCode, details);
    }
}
