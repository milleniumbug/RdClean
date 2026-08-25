using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sail;
using Sail.ComfyUi;
using Sail.ComfyUi.Models;

namespace RdClean.Domain;

public class RedrawService(
    ComfyUiClient comfyUiClient,
    ILogger<RedrawService> logger)
{
    private readonly ILogger<RedrawService> logger = logger;

    public async Task<Stream> Redraw(Stream imageStream, Stream? maskStream, string name, Rectangle2D area)
    {
        var inputs = new RedrawInputs()
        {
            InputImage = imageStream,
            MaskImage = maskStream,
            Area = area,
        };
        var rdWorkflow = CreateRedrawWorkflow(inputs);
        await using var preprocessedImageStream = await rdWorkflow.PreProcessInput(inputs);
        var uploadResponse = await comfyUiClient.UploadImage(name, preprocessedImageStream);

        inputs.RewindStreams();
        var comfyWorkflow = await rdWorkflow.CreateWorkflow(inputs, uploadResponse.Name);
        logger.LogInformation("Workflow used: {Workflow}", JsonSerializer.Serialize(comfyWorkflow));
        
        var promptResponse = await comfyUiClient.Prompt(
            new PromptRequest(
                comfyWorkflow));

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
        return await rdWorkflow.PostProcessOutput(inputs, output);
    }

    private IRedrawWorkflow CreateRedrawWorkflow(RedrawInputs inputs)
    {
        return inputs.MaskImage != null
            ? new RedrawWorkflowColorMask("magenta")
            : new RedrawWorkflowRemoveText();
    }
}