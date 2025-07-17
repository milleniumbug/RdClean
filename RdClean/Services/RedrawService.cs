using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using RdClean.Data;
using Sail;
using Sail.ComfyUi;
using Sail.ComfyUi.Models;

namespace RdClean.Services;

public class RedrawService(
    ComfyUiClient comfyUiClient,
    IOptions<RedrawServiceConfiguration> configuration,
    ILogger<RedrawService> logger)
{
    private readonly ILogger<RedrawService> logger = logger;
    
    private readonly RedrawServiceConfiguration config = configuration.Value;

    private readonly RedrawWorkflow redrawWorkflow = new RedrawWorkflowColorMask("magenta");
    
    public async Task<Stream> Redraw(Stream imageStream, Stream maskStream, string name, Rectangle2D area)
    {
        var inputs = new RedrawInputs()
        {
            InputImage = imageStream,
            MaskImage = maskStream,
            Area = area,
        };
        await using var preprocessedImageStream = await redrawWorkflow.PreProcessInput(inputs);
        var uploadResponse = await comfyUiClient.UploadImage(name, preprocessedImageStream);

        inputs.RewindStreams();
        var workflow = await redrawWorkflow.CreateWorkflow(inputs, uploadResponse.Name);
        logger.LogInformation("Workflow used: {Workflow}", JsonSerializer.Serialize(workflow));
        
        var promptResponse = await comfyUiClient.Prompt(
            new PromptRequest(
                workflow));

        HistoryEntry promptResult;
        while (true)
        {
            var queueResponse = await comfyUiClient.GetQueue();
            bool isInQueue = queueResponse.Pending
                .Concat(queueResponse.Running)
                .Any(entry => entry.PromptId == promptResponse.PromptId);

            var historyResponse = await comfyUiClient.GetHistory(maxItems: 64);
            var historyEntry = historyResponse.HistoryEntries.GetValueOrDefault(promptResponse.PromptId);
            if (historyEntry != null)
            {
                promptResult = historyEntry;
                break;
            }
            else if(!isInQueue)
            {
                throw new IOException();
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        var resource = promptResult.Outputs
            .SelectMany(output => output.Value.Images)
            .First(img => img.Type == "output");

        var output = await comfyUiClient.ViewFile(resource);
        inputs.RewindStreams();
        return await redrawWorkflow.PostProcessOutput(inputs, output);
    }
}