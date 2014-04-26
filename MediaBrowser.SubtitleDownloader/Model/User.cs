namespace MediaBrowser.SubtitleDownloader.Model
{
    public class User
    {
        public string LinkedMbUserId { get; set; }

        public string[] SubtitleLocations { get; set; }

        public string SubtitleLanguage { get; set; }
    }
}