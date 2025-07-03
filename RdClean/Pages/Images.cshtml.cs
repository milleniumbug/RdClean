using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RdClean.Data;
using RdClean.Extensions;

namespace RdClean.Pages;

[Authorize]
public class ImageModel : PageModel
{
    private readonly ILogger<ImageModel> _logger;
    private readonly ApplicationDbContext dbContext;

    public ImageModel(ILogger<ImageModel> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        this.dbContext = dbContext;
    }
    
    public async Task OnGetAsync(Guid? id = null)
    {
        if (id != null)
        {
            this.Image = await dbContext.Images
                .AsSplitQuery()
                .Include(image => image.Redraws)
                .Select(image => new ImageDownloadModel()
                {
                    Id = image.Id,
                    Width = image.Width,
                    Height = image.Height,
                    Redraws = image.Redraws!.Select(
                        redraw => new RedrawDownloadModel()
                        {
                            X = redraw.X,
                            Y = redraw.Y,
                            Width = redraw.Width,
                            Height = redraw.Height,
                            RedrawId = redraw.Id,
                            IsReady = redraw.ImageBytes != null,
                        })
                })
                .FirstOrDefaultAsync(image => image.Id == id);
        }
    }
    
    public async Task<IActionResult> OnGetImageDataAsync(
        Guid id)
    {
        var image = await dbContext.Images
            .FirstOrDefaultAsync(image => image.Id == id);
        if (image == null)
        {
            return NotFound();
        }
        return File(image.ImageBytes, "image/png");
    }
    
    public async Task<IActionResult> OnPostAsync(
        [FromForm] ImageUploadModel image)
    {
        if (image.Image.Length > 150 * 1024 * 1024)
        {
            return BadRequest();
        }

        var userName = Request.GetUserName();
        var user = await dbContext.Users.FirstOrDefaultAsync(user => user.UserName == userName);
        if (user == null)
        {
            return Forbid();
        }
        
        await using var stream = image.Image.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var imageBytes = memoryStream.ToArray();

        var formatDetect = SixLabors.ImageSharp.Image.DetectFormat(imageBytes);
        var identify = SixLabors.ImageSharp.Image.Identify(imageBytes);
        
        var entity = new Image(
            user,
            imageBytes,
            image.Image.FileName,
            formatDetect.DefaultMimeType,
            identify.Width,
            identify.Height);
        dbContext.Images.Add(
            entity);
        
        await dbContext.SaveChangesAsync();
        
        return RedirectToPage(new { id = entity.Id });
    }

    public ImageDownloadModel? Image { get; set; }
}

public class ImageDownloadModel
{
    public Guid Id { get; set; }
    
    public int Width { get; set; }
    
    public int Height { get; set; }
    
    public IEnumerable<RedrawDownloadModel> Redraws { get; set; }
}

public class RedrawDownloadModel
{
    public int X { get; set; }
    
    public int Y { get; set; }
    
    public int Width { get; set; }
    
    public int Height { get; set; }
    
    public Guid RedrawId { get; set; }
    
    public bool IsReady { get; set; }
}

public class ImageUploadModel
{
    public IFormFile Image { get; set; }
}