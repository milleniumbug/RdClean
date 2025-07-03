namespace RdClean.Controllers;

public class AddRedrawRequest
{
    public Guid ImageId { get; set; }
    
    public int X { get; set; }
    
    public int Y { get; set; }
    
    public int Width { get; set; }
    
    public int Height { get; set; }
}