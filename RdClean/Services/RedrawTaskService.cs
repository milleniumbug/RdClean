using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RdClean.Data;
using Sail;

namespace RdClean.Services;

public class RedrawTaskService(
    ILogger<RedrawTaskService> logger,
    IServiceScopeFactory scopeFactory) : IHostedService, IDisposable
{
    private readonly SemaphoreSlim semaphore = new(0, 1);
    private CancellationTokenSource? cts;
    private Task? task;

    public Task StartAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{ServiceName} service running", this.GetType().Name);

        cts = new CancellationTokenSource();
        task = Task.Run(async () =>
        {
            await DoWork(cts.Token);
        }, stoppingToken);

        return Task.CompletedTask;
    }

    public void Wake()
    {
        try
        {
            this.semaphore.Release(1);
        }
        catch (SemaphoreFullException)
        {
            
        }
    }
    
    private async Task DoWork(CancellationToken cancellationToken)
    {
        await this.semaphore.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        while (true)
        {
            using var scope = scopeFactory.CreateScope();
            while (await DoWork(scope.ServiceProvider, cancellationToken))
            {
                
            }
            await this.semaphore.WaitAsync(TimeSpan.FromHours(1), cancellationToken);
        }
    }

    private async Task<bool> DoWork(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var redrawService = serviceProvider.GetRequiredService<RedrawService>();
        var fileProvider = serviceProvider.GetRequiredService<IFileProvider>();
        var redraw = await dbContext.Redraws
            .Include(redraw => redraw.Image)
            .FirstOrDefaultAsync(
                redraw => redraw.FileId == null,
                cancellationToken: cancellationToken);
        if (redraw == null)
        {
            return false;
        }

        try
        {
            await IssueComfy(redrawService, fileProvider, redraw);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occured during redrawing");
            return false;
        }
        
        return true;
    }

    private async Task IssueComfy(RedrawService redrawService, IFileProvider provider, Redraw redraw)
    {
        await using var inputStream = await provider.Download(redraw.Image.FileId);
        await using var maskStream = redraw.Image.MaskFileId != null
            ? await provider.Download(redraw.Image.MaskFileId.Value)
            : Stream.Null;
        var outputStream = await redrawService.Redraw(
            inputStream,
            maskStream,
            redraw.Image.Name,
            new Rectangle2D(
                new Point2D(redraw.X, redraw.Y),
                new Size2D(redraw.Width, redraw.Height)));

        var redrawFileId = await provider.Upload(outputStream);
        redraw.SetRedraw(redrawFileId);
    }
    
    public async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{ServiceName} is stopping", this.GetType().Name);

        if (cts != null)
        {
            await cts.CancelAsync();
            cts.Dispose();
        }
        
        if (task != null)
        {
            await task;
        }
    }

    public void Dispose()
    {
        cts?.Dispose();
    }
}