using JetBrains.Annotations;

namespace RdClean.Data;

public class Redraw
{
    public Guid Id { get; private set; }
    
    public Guid? FileId { get; private set; }
    
    public int X { get; private set; }
    
    public int Y { get; private set; }
    
    public int Width { get; private set; }
    
    public int Height { get; private set; }
    
    public Image Image { get; private set; }
    
    public DateTime CreatedAt { get; private set; }

    [UsedImplicitly]
    private Redraw()
    {
        Image = null!;
    }

    public Redraw(Image image, int x, int y, int width, int height)
    {
        CreatedAt = DateTime.UtcNow;
        Id = Guid.NewGuid();
        Image = image;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public void SetRedraw(Guid fileId)
    {
        FileId = fileId;
    }
}