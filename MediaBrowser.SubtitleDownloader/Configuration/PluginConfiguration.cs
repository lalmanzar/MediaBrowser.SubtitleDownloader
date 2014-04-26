using System;
using MediaBrowser.Model.Plugins;
using MediaBrowser.SubtitleDownloader.Model;

namespace MediaBrowser.SubtitleDownloader.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            Users = new User[] { };
            DownloadedSubtitles = new Subtitle []{};
        }

        public Subtitle[] DownloadedSubtitles { get; set; }
        public User[] Users { get; set; }
    }

    public class Subtitle {
        public string IdSubtitle { get; set; }

        public string SubtitleFileName { get; set; }

        public DateTime Date { get; set; }
    }
}
