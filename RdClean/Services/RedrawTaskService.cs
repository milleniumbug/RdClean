using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RdClean.Data;

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
        while (true)
        {
            await this.semaphore.WaitAsync(TimeSpan.FromHours(1), cancellationToken);
            using var scope = scopeFactory.CreateScope();
            while (await DoWork(scope.ServiceProvider, cancellationToken))
            {
                
            }
        }
    }

    private async Task<bool> DoWork(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var redraw = await dbContext.Redraws
            .Include(redraw => redraw.Image)
            .FirstOrDefaultAsync(
                redraw => redraw.ImageBytes == null,
                cancellationToken: cancellationToken);
        if (redraw == null)
        {
            return false;
        }

        try
        {
            await IssueComfy(redraw);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occured during redrawing");
            return false;
        }
        
        return true;
    }

    private async Task IssueComfy(Redraw redraw)
    {
        throw new NotImplementedException();
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