namespace SamsGameLauncher.Models
{
    // Represents a configured emulator and its launch settings
    public class Emulator
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string DefaultArguments { get; set; } = string.Empty;
        public string TargetConsole { get; set; } = string.Empty;
    }
}