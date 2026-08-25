using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using RdClean.Domain.Extensions;
using Sail;
using Sail.ComfyUi.Models;

namespace RdClean.Domain;

public abstract class Flux2RedrawWorkflow : IRedrawWorkflow
{
    protected async Task<Workflow> CreateWorkflowGeneric(string textPrompt, string inputImageName, Rectangle2D area)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        //var a = assembly.GetManifestResourceNames();
        var workflowName = (area.Width, area.Height) switch
        {
            (1024, 1024) => "RdClean.Domain.Flows.flux_2_klein_redraw_1024.json",
            _ => throw new ArgumentOutOfRangeException(nameof(area), area, null)
        };
        await using Stream responseStream = assembly.GetManifestResourceStream(workflowName) ?? throw new InvalidOperationException();
        var workflow = await JsonSerializer.DeserializeAsync<JsonNode>(responseStream) ?? throw new InvalidOperationException();

        workflow["6"]!["inputs"]!["text"] = textPrompt;
        workflow["57"]!["inputs"]!["x"] = area.TopLeft.X;
        workflow["57"]!["inputs"]!["y"] = area.TopLeft.Y;
        workflow["60"]!["inputs"]!["x_offset"] = area.TopLeft.X;
        workflow["60"]!["inputs"]!["y_offset"] = area.TopLeft.Y;
        workflow["42"]!["inputs"]!["image"] = inputImageName;
        
        return JsonSerializer.Deserialize<Workflow>(JsonSerializer.Serialize(workflow))!;
    }
    
    public abstract Task<Stream> PreProcessInput(RedrawInputs inputs);

    public abstract Task<Workflow> CreateWorkflow(RedrawInputs inputs, string preprocessedInputImageName);

    public abstract Task<Stream> PostProcessOutput(RedrawInputs inputs, Stream outputImageStream);
}

public class Flux2RedrawWorkflowRemoveText : Flux2RedrawWorkflow
{
    public override async Task<Stream> PreProcessInput(RedrawInputs inputs)
    {
        var stream = FileExt.CreateTemporaryFile();
        await inputs.InputImage.CopyToAsync(stream);
        stream.Position = 0;
        return stream;
    }

    public override async Task<Workflow> CreateWorkflow(RedrawInputs inputs, string preprocessedInputImageName)
    {
        return await CreateWorkflowGeneric(
            "Remove all japanese text from the image. Leave everything else unchanged.",
            preprocessedInputImageName,
            inputs.Area);
    }

    public override Task<Stream> PostProcessOutput(RedrawInputs inputs, Stream outputImageStream)
    {
        return Task.FromResult(outputImageStream);
    }
}