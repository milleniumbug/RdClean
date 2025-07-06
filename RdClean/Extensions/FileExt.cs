namespace RdClean.Extensions;

public class FileExt
{
    public static Stream CreateTemporaryFile()
    {
        return new FileStream(Path.GetTempFileName(), new FileStreamOptions()
        {
            Access = FileAccess.ReadWrite,
            Mode = FileMode.Create,
            Share = FileShare.Delete,
            Options = FileOptions.Asynchronous | FileOptions.DeleteOnClose,
        });
    }
}