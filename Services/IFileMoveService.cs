namespace SamsGameLauncher.Services
{
    public interface IFileMoveService
    {
        Task<bool> MoveFolderAsync(string source,
                                   string destination,
                                   IProgress<MoveProgress> progress,
                                   CancellationToken token);
    }

    public record MoveProgress(double Percent, string? CurrentFile);
}