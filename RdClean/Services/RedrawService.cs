using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using RdClean.Data;
using Sail;
using Sail.ComfyUi;
using Sail.ComfyUi.Models;

namespace RdClean.Services;

public class RedrawService(ComfyUiClient comfyUiClient)
{
    public async Task<Workflow> CreateWorkflow(string inputImageName, Rectangle2D area)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        var a = assembly.GetManifestResourceNames();
        var workflowName = (area.Width, area.Height) switch
        {
            (1024, 1024) => "RdClean.Services.Flows.flux_1_kontext_redraw_4_1024.json",
            (2048, 2048) => "RdClean.Services.Flows.flux_1_kontext_redraw_4_2048.json",
            (4096, 4096) => "RdClean.Services.Flows.flux_1_kontext_redraw_4_4096.json",
            _ => throw new ArgumentOutOfRangeException(nameof(area), area, null)
        };
        await using Stream responseStream = assembly.GetManifestResourceStream(workflowName) ?? throw new InvalidOperationException();
        var workflow = await JsonSerializer.DeserializeAsync<JsonNode>(responseStream) ?? throw new InvalidOperationException();

        workflow["198"]!["inputs"]!["int"] = area.TopLeft.X;
        workflow["199"]!["inputs"]!["int"] = area.TopLeft.Y;
        workflow["246"]!["inputs"]!["image"] = inputImageName;
        
        return JsonSerializer.Deserialize<Workflow>(JsonSerializer.Serialize(workflow))!;
    }
    
    public async Task<Stream> Redraw(Stream imageStream, string name, Rectangle2D area)
    {
        var uploadResponse = await comfyUiClient.UploadImage(name, imageStream);

        var workflow = await CreateWorkflow(name, area);
        
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

        return await comfyUiClient.ViewFile(resource);
    }
}