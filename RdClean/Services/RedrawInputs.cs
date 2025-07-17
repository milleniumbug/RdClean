using Sail;

namespace RdClean.Services;

public record RedrawInputs
{
    public required Stream InputImage { get; init; }

    public required Stream MaskImage { get; init; }

    public required Rectangle2D Area { get; init; }

    public void RewindStreams()
    {
        InputImage.Position = 0;
        MaskImage.Position = 0;
    }
}