using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RdClean.Extensions;
using RdClean.Services;

namespace RdClean.Controllers;

public class EndpointsController : ControllerBase
{
    [Authorize]
    [HttpPost("Image/Delete/{id}")]
    public async Task<IActionResult> DeleteImagePost(
        [FromRoute] Guid id,
        [FromServices] DeleteService service)
    {
        var result = await service.DeleteImage(
            Request.GetUserName(),
            id);

        return result.Match<IActionResult>(
            some => RedirectToPage("/Images"),
            none => none switch
            {
                HttpStatusCode.NotFound => RedirectToPage("/Images"),
                HttpStatusCode.Forbidden => Forbid(),
                _ => throw new InvalidOperationException(),
            });
    }
}