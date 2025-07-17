using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sail;
using Sail.ComfyUi.Models;

namespace RdClean.Services;

public abstract class RedrawWorkflow
{
    protected async Task<Workflow> CreateWorkflowGeneric(string textPrompt, string inputImageName, Rectangle2D area)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        //var a = assembly.GetManifestResourceNames();
        var workflowName = (area.Width, area.Height) switch
        {
            (1024, 1024) => "RdClean.Services.Flows.flux_1_kontext_redraw_4_1024.json",
            (2048, 2048) => "RdClean.Services.Flows.flux_1_kontext_redraw_4_2048.json",
            (4096, 4096) => "RdClean.Services.Flows.flux_1_kontext_redraw_4_4096.json",
            _ => throw new ArgumentOutOfRangeException(nameof(area), area, null)
        };
        await using Stream responseStream = assembly.GetManifestResourceStream(workflowName) ?? throw new InvalidOperationException();
        var workflow = await JsonSerializer.DeserializeAsync<JsonNode>(responseStream) ?? throw new InvalidOperationException();

        workflow["6"]!["inputs"]!["text"] = textPrompt;
        workflow["198"]!["inputs"]!["int"] = area.TopLeft.X;
        workflow["199"]!["inputs"]!["int"] = area.TopLeft.Y;
        workflow["246"]!["inputs"]!["image"] = inputImageName;
        
        return JsonSerializer.Deserialize<Workflow>(JsonSerializer.Serialize(workflow))!;
    }
    
    public abstract Task<Stream> PreProcessInput(RedrawInputs inputs);

    public abstract Task<Workflow> CreateWorkflow(RedrawInputs inputs, string preprocessedInputImageName);

    public abstract Task<Stream> PostProcessOutput(RedrawInputs inputs, Stream outputImageStream);
}