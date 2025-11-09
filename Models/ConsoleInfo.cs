namespace Moodex.Models
{
    public class ConsoleInfo
    {
        // Console data loaded/saved to settings.json
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        // Cover aspect ratio (Width:Height). Defaults to 1:1 (supports decimals)
        public double CoverAspectW { get; set; } = 1.0;
        public double CoverAspectH { get; set; } = 1.0;
    }
}

