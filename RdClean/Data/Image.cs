using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace RdClean.Data;

public class Image
{
    public Guid Id { get; private set; }
    
    public User User { get; private set; }
    
    [MinLength(1)]
    [MaxLength(256)]
    public string Name { get; private set; }
    
    [MaxLength(256)]
    public string MimeType { get; private set; }
    
    public Guid FileId { get; private set; }
    
    public int Width { get; private set; }
    
    public int Height { get; private set; }
    
    public IReadOnlyCollection<Redraw>? Redraws { get; private set; }

    [UsedImplicitly]
    private Image()
    {
        Name = null!;
        User = null!;
        MimeType = null!;
    }

    public Image(User user, Guid fileId, string name, string mimeType, int width, int height)
    {
        Id = Guid.NewGuid();
        User = user;
        FileId = fileId;
        Name = name;
        MimeType = mimeType;
        Width = width;
        Height = height;
    }
}