using System.Net;
using Microsoft.EntityFrameworkCore;
using Optional;
using RdClean.Data;

namespace RdClean.Services;

public class DeleteService(
    ApplicationDbContext dbContext,
    IFileProvider fileProvider,
    ILogger<DeleteService> logger)
{
    public async Task<Option<ValueTuple, HttpStatusCode>> DeleteImage(
        string? userName,
        Guid id)
    {
        if (userName == null)
        {
            return Option.None<ValueTuple, HttpStatusCode>(HttpStatusCode.Forbidden);
        }

        bool imageBelongsToUser = await dbContext.Images
            .AnyAsync(image =>
                image.Id == id &&
                image.User.UserName == userName);

        if (!imageBelongsToUser)
        {
            return Option.None<ValueTuple, HttpStatusCode>(HttpStatusCode.NotFound);
        }

        var filesToDelete = await dbContext.Redraws
            .Where(redraw => redraw.Image.Id == id)
            .Where(redraw => redraw.FileId != null)
            .Select(redraw => redraw.FileId!.Value)
            .ToListAsync();

        await dbContext.Redraws
            .Where(redraw => redraw.Image.Id == id)
            .ExecuteDeleteAsync();

        foreach (var fileId in filesToDelete)
        {
            try
            {
                await fileProvider.Delete(fileId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "deleting file failed");
            }
        }
        
        filesToDelete = (await dbContext.Images
                .Where(image => image.Id == id)
                .Select(image => new { image.FileId, image.MaskFileId })
                .ToListAsync())
            .SelectMany(files => files.MaskFileId != null
                ? new [] { files.FileId, files.MaskFileId.Value }
                : new [] { files.FileId })
            .ToList();
        
        await dbContext.Images
            .Where(image => image.Id == id)
            .ExecuteDeleteAsync();
        
        foreach (var fileId in filesToDelete)
        {
            try
            {
                await fileProvider.Delete(fileId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "deleting file failed");
            }
        }

        return Option.Some<ValueTuple, HttpStatusCode>(ValueTuple.Create());
    }
}