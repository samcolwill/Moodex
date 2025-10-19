namespace SamsGameLauncher.Services
{
    public interface IFileMoveService
    {
        Task<bool> MoveFolderAsync(string source,
                                   string destination,
                                   IProgress<MoveProgress> progress,
                                   CancellationToken token,
                                   bool compressToArchive = false,
                                   string? sevenZipPath = null);
    }

    public record MoveProgress(double Percent, string? CurrentFile);
}