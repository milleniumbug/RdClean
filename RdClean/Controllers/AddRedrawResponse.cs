namespace RdClean.Controllers;

public class AddRedrawResponse
{
    public Guid ImageId { get; init; }
    
    public Guid RedrawId { get; init; }
    
    public required int X { get; init; }
    
    public required int Y { get; init; }
    
    public required int Width { get; init; }
    
    public required int Height { get; init; }
}