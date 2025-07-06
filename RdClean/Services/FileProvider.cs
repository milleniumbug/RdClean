namespace RdClean.Services;

public class FileProvider : IFileProvider
{
    private readonly DirectoryInfo rootDirectory;

    public FileProvider(DirectoryInfo rootDirectory)
    {
        this.rootDirectory = rootDirectory;
    }
    
    private string GetPathFromId(Guid id)
    {
        return Path.Combine(this.rootDirectory.FullName,  $"{id}");
    }

    public Task<Stream> Download(Guid id)
    {
        var path = GetPathFromId(id);
        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }
    
    public async Task<Guid> Upload(Stream stream)
    {
        var id = Guid.NewGuid();
        var path = GetPathFromId(id);
        try
        {
            await using var file = File.OpenWrite(path);
            await stream.CopyToAsync(file);
        }
        catch (Exception ex)
        {
            File.Delete(path);
            throw;
        }

        return id;
    }

    public Task Delete(Guid id)
    {
        var path = GetPathFromId(id);
        File.Delete(path);
        return Task.CompletedTask;
    }
}