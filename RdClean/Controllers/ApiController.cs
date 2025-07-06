using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RdClean.Data;
using RdClean.Extensions;
using RdClean.Services;

namespace RdClean.Controllers;

[ApiController]
[Route("Api")]
public class ApiController : ControllerBase
{
    [Authorize]
    [HttpPost("Redraw")]
    public async Task<IActionResult> Redraw(
        [FromBody] AddRedrawRequest request,
        [FromServices] ApplicationDbContext dbContext,
        [FromServices] RedrawTaskService redrawTaskService)
    {
        if (request.Width is not (1024 or 2048 or 4096) ||
            request.Width != request.Height)
        {
            return BadRequest();
        }

        var userName = Request.GetUserName();
        if (userName == null)
        {
            return Forbid();
        }

        var image = await dbContext.Images
            .FirstOrDefaultAsync(image =>
                image.Id == request.ImageId &&
                image.User.UserName == userName);

        if (image == null)
        {
            return NotFound();
        }

        int w = request.Width;
        int h = request.Height;
        
        int x = Math.Clamp(request.X, 0, image.Width - request.Width);
        int y = Math.Clamp(request.Y, 0, image.Height - request.Height);

        var redrawEntity = new Redraw(image, x, y, w, h);
        dbContext.Redraws.Add(redrawEntity);
        
        await dbContext.SaveChangesAsync();
        
        redrawTaskService.Wake();

        return new JsonResult(
            new AddRedrawResponse()
            {
                ImageId = request.ImageId,
                RedrawId = redrawEntity.Id,
                RedrawUrl = Url.Page("/Redraw", "ImageData", new { id = redrawEntity.Id }),
                Width = request.Width,
                Height = request.Height,
                X = x,
                Y = y,
            });
    }
}