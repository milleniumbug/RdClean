using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RdClean.Data;
using RdClean.Extensions;
using RdClean.Services;

namespace RdClean.Pages;

[Authorize]
public class RedrawModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ApplicationDbContext dbContext;
    private readonly IFileProvider fileProvider;

    public RedrawModel(
        ILogger<IndexModel> logger,
        ApplicationDbContext dbContext,
        IFileProvider fileProvider)
    {
        _logger = logger;
        this.dbContext = dbContext;
        this.fileProvider = fileProvider;
    }

    public void OnGet()
    {
    }
    
    public async Task<IActionResult> OnGetImageDataAsync(
        Guid id)
    {
        var userName = Request.GetUserName();
        if (userName == null)
        {
            return Forbid();
        }

        var redraw = await dbContext.Redraws
            .FirstOrDefaultAsync(redraw =>
                redraw.Id == id &&
                redraw.FileId != null &&
                redraw.Image.User.UserName == userName);

        if (redraw == null)
        {
            return NotFound();
        }

        var file = await fileProvider.Download(redraw.FileId!.Value);
        return File(file, "image/png");
    }
}
