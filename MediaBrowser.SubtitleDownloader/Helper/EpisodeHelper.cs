using System.Globalization;
using System.Text.RegularExpressions;

namespace MediaBrowser.SubtitleDownloader.Helper {
    public class EpisodeHelper {
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        public static int? GetEpisodeNumber(string path)
        {
            string fl = path.ToLower();
            foreach (var r in EpisodeExpressions)
            {
                Match m = r.Match(fl);
                if (m.Success)
                    return ParseEpisodeNumber(m.Groups["epnumber"].Value);
            }
            return null;
        }
        private static int? ParseEpisodeNumber(string val)
        {
            int num;

            if (!string.IsNullOrEmpty(val) && int.TryParse(val, NumberStyles.Integer, UsCulture, out num))
            {
                return num;
            }

            return null;
        }

        public static int? GetSeasonNumber(string path)
        {
            string fl = path.ToLower();
            foreach (var r in EpisodeExpressions)
            {
                Match m = r.Match(fl);
                if (m.Success)
                    return ParseEpisodeNumber(m.Groups["seasonnumber"].Value);
            }
            return null;
        }

        private static readonly Regex[] EpisodeExpressions =
        {
            new Regex(
                @"^[sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3})[^\\\/]*$",
                RegexOptions.Compiled),
            new Regex(
                @"^[sS](?<seasonnumber>\d{1,4})[x,X]?[eE](?<epnumber>\d{1,3})[^\\\/]*$",
                RegexOptions.Compiled),
            new Regex(
                @"^(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3}))[^\\\/]*$",
                RegexOptions.Compiled),
            new Regex(
                @"^(?<seriesname>[^\\\/]*)[sS](?<seasonnumber>\d{1,4})[xX\.]?[eE](?<epnumber>\d{1,3})[^\\\/]*$",
                RegexOptions.Compiled)
        }; 
    }
}