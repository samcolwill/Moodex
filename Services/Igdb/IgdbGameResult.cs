namespace Moodex.Services.Igdb
{
    public class IgdbGameResult
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Genres { get; set; } = new();
        public DateTime? ReleaseDate { get; set; }
        public string? CoverImageId { get; set; }
        public string? Summary { get; set; }
    }
}
