using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RdClean.Data;
using RdClean.Extensions;
using RdClean.Services;

namespace RdClean.Pages;

[Authorize]
public class ImageModel : PageModel
{
    private readonly ILogger<ImageModel> _logger;
    private readonly ApplicationDbContext dbContext;
    private readonly IFileProvider fileProvider;

    public ImageModel(ILogger<ImageModel> logger, ApplicationDbContext dbContext, IFileProvider fileProvider)
    {
        _logger = logger;
        this.dbContext = dbContext;
        this.fileProvider = fileProvider;
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
                            IsReady = redraw.FileId != null,
                        })
                })
                .FirstOrDefaultAsync(image => image.Id == id);
        }
        else
        {
            this.Images = (await dbContext.Images
                    .Select(image => new { image.Name, image.Id })
                    .ToListAsync())
                .Select(image => (image.Name, Url.Page("/Images", new { id = image.Id })))
                .ToList();
        }
    }
    
    public async Task<IActionResult> OnGetImageDataAsync(
        Guid id)
    {
        var userName = Request.GetUserName();
        if (userName == null)
        {
            return Forbid();
        }

        var image = await dbContext.Images
            .FirstOrDefaultAsync(image =>
                image.Id == id &&
                image.User.UserName == userName);

        if (image == null)
        {
            return NotFound();
        }
        
        var fileStream = await fileProvider.Download(image.FileId);
        return File(fileStream, image.MimeType);
    }
    
    public async Task<IActionResult> OnGetDownloadAsync(
        Guid id)
    {
        var userName = Request.GetUserName();
        if (userName == null)
        {
            return Forbid();
        }

        var image = await dbContext.Images
            .Include(image => image.Redraws)
            .FirstOrDefaultAsync(image =>
                image.Id == id &&
                image.User.UserName == userName);

        if (image == null)
        {
            return NotFound();
        }
        
        var memoryStream = new MemoryStream();
        using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            {
                var zipArchiveEntry = zip.CreateEntry(image.Name, CompressionLevel.Fastest);
                await using var zipStream = zipArchiveEntry.Open();
                await using var imageStream = await fileProvider.Download(image.FileId);
                await imageStream.CopyToAsync(zipStream);
            }
            int i = 0;
            foreach (var redraw in image.Redraws ?? [])
            {
                i++;
                if (redraw.FileId == null)
                {
                    continue;
                }

                var zipArchiveEntry = zip.CreateEntry($"{i:D5}.png", CompressionLevel.Fastest);
                await using var zipStream = zipArchiveEntry.Open();
                await using var redrawStream = await fileProvider.Download(redraw.FileId.Value);
                await redrawStream.CopyToAsync(zipStream);
            }
        }

        memoryStream.Position = 0;

        return File(memoryStream, "application/zip");
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
        await using var tempFile = FileExt.CreateTemporaryFile();
        await stream.CopyToAsync(tempFile);

        tempFile.Position = 0;
        var formatDetect = await SixLabors.ImageSharp.Image.DetectFormatAsync(tempFile);
        tempFile.Position = 0;
        var identify = await SixLabors.ImageSharp.Image.IdentifyAsync(tempFile);
        tempFile.Position = 0;
        var fileId = await fileProvider.Upload(tempFile);

        var entity = new Image(
            user,
            fileId,
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
    
    public IEnumerable<(string name, string? url)>? Images { get; set; }
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