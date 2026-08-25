using Sail;

namespace RdClean.Domain;

public record RedrawInputs
{
    public required Stream InputImage { get; init; }

    public required Stream? MaskImage { get; init; }

    public required Rectangle2D Area { get; init; }

    public void RewindStreams()
    {
        InputImage.Position = 0;
        if (MaskImage != null)
        {
            MaskImage.Position = 0;
        }
    }
}