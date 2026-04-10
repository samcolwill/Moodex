namespace Moodex.Services.Igdb
{
    public interface IIgdbService
    {
        bool IsEnabled { get; }
        Task<List<IgdbGameResult>> SearchGameAsync(string name);
        Task<byte[]?> DownloadCoverAsync(string imageId);
    }
}
