namespace RdClean.Services;

public interface IFileProvider
{
    Task<Stream> Download(Guid id);
    Task<Guid> Upload(Stream stream);
    Task Delete(Guid id);
}