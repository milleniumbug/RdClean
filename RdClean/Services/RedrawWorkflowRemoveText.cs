using RdClean.Extensions;
using Sail.ComfyUi.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace RdClean.Services;

public class RedrawWorkflowRemoveText : RedrawWorkflow
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
            "Remove the japanese text from this manga panel. Leave everything else unchanged!",
            preprocessedInputImageName,
            inputs.Area);
    }

    public override Task<Stream> PostProcessOutput(RedrawInputs inputs, Stream outputImageStream)
    {
        return Task.FromResult(outputImageStream);
    }
}

public class RedrawWorkflowColorMask : RedrawWorkflow
{
    private readonly string textPrompt;

    public RedrawWorkflowColorMask(
        string colorName)
    {
        this.textPrompt = $"Remove the {colorName} colored areas. Keep everything else unchanged.";
    }
    
    public override async Task<Stream> PreProcessInput(RedrawInputs inputs)
    {
        var stream = FileExt.CreateTemporaryFile();
        var image = await Image.LoadAsync(inputs.InputImage);
        var mask = await Image.LoadAsync(inputs.MaskImage);
        image.Mutate(i =>
        {
            i.DrawImage(mask, 1.0f);
        });
        await image.SaveAsync(stream, PngFormat.Instance);
        stream.Position = 0;
        return stream;
    }

    public override async Task<Workflow> CreateWorkflow(RedrawInputs inputs, string preprocessedInputImageName)
    {
        return await CreateWorkflowGeneric(
            textPrompt,
            preprocessedInputImageName,
            inputs.Area);
    }

    public override Task<Stream> PostProcessOutput(RedrawInputs inputs, Stream outputImageStream)
    {
        return Task.FromResult(outputImageStream);
    }
}