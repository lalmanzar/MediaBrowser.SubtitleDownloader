using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.SubtitleDownloader.Configuration;
using MediaBrowser.SubtitleDownloader.Helper;
using OpenSubtitlesHandler;

namespace MediaBrowser.SubtitleDownloader.Provider {
    class SubtitleProvider : ICustomMetadataProvider<Episode>, IHasOrder
    {
        private readonly IUserManager _userManager;
        readonly ILogger _logger;

        public SubtitleProvider(IUserManager userManager, ILogger logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public int Order
        {
            get
            {
                return 90;
            }
        }

        public Task<ItemUpdateType> FetchAsync(Episode item, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            return GetSubtitle(item, cancellationToken);
        }

        public async Task<ItemUpdateType> GetSubtitle(Episode item, CancellationToken cancellationToken) {
            _logger.Debug("Start with: " + item.Name);
            if (!Supports(item))
            {
                _logger.Debug("Not Supported");
                return ItemUpdateType.None;
            }
            _logger.Debug("Supported");

            if(!item.IndexNumber.HasValue || !item.ParentIndexNumber.HasValue)
            {
                _logger.Debug("Information Missing");
                return ItemUpdateType.None;
            }
            if (string.IsNullOrEmpty(item.Path)) {
                _logger.Debug("Path Missing");
                return ItemUpdateType.None;
            }
            OpenSubtitles.SetUserAgent("OS Test User Agent");
            var loginResponse = OpenSubtitles.LogIn("ruisu15", "migueru15", "en");
            if (!(loginResponse is MethodResponseLogIn))
            {
                _logger.Debug("Login error");
                return ItemUpdateType.None;
            }
            var user = _userManager.Users.Where(u =>
            {
                var subtitleUser = UserHelper.GetUser(u);

                return subtitleUser != null && subtitleUser.SubtitleLocations != null &&
                       subtitleUser.SubtitleLocations.Length > 0;

            }).Select(UserHelper.GetUser).First();
            var subLanguageId = user.SubtitleLanguage;
            _logger.Debug("User language: " + subLanguageId);
            var hash = Utilities.ComputeHash(item.Path);
            var fileInfo = new FileInfo(item.Path);
            var movieByteSize = fileInfo.Length;

            _logger.Debug(string.Format("{0} - {1} - {2}", subLanguageId, hash, movieByteSize));
            _logger.Debug(string.Format("{0} - {1} - {2} - {3}", subLanguageId, item.SeriesName, item.ParentIndexNumber.Value.ToString(), item.IndexNumber.Value.ToString()));

            var parms = new List<SubtitleSearchParameters> {
                                                               new SubtitleSearchParameters(subLanguageId, hash, movieByteSize),
                                                               new SubtitleSearchParameters(subLanguageId, item.SeriesName, item.ParentIndexNumber.Value.ToString(), item.IndexNumber.Value.ToString()),

                                                           };
            var result = OpenSubtitles.SearchSubtitles(parms.ToArray());
            if (!(result is MethodResponseSubtitleSearch))
            {
                _logger.Debug("invalid response type");
                return ItemUpdateType.None;
            }
            var downloadedSubtitles = new List<Subtitle>(Plugin.Instance.PluginConfiguration.DownloadedSubtitles);
            var results = ((MethodResponseSubtitleSearch)result).Results;
            var bestResult = results.Where(x => x.SubBad == "0" && int.Parse(x.SeriesSeason) == item.ParentIndexNumber && int.Parse(x.SeriesEpisode) == item.IndexNumber)
                    .Where(x => downloadedSubtitles.All(y => y.IdSubtitle != x.IDSubtitle))
                    .OrderBy(x => x.MovieHash == hash)
                    .ThenBy(x => Math.Abs(long.Parse(x.MovieByteSize) - movieByteSize))
                    .ThenByDescending(x => int.Parse(x.SubDownloadsCnt))
                    .ThenByDescending(x => double.Parse(x.SubRating))
                    .ToList();
            if (!bestResult.Any())
            {
                _logger.Debug("No Subtitles");
                return ItemUpdateType.None;
            }
            _logger.Debug("Found " + bestResult.Count + " subtitles.");
           
            var subtitle = bestResult.First();
            var downloadsList = new[] { int.Parse(subtitle.IDSubtitleFile) };

            var resultDownLoad = OpenSubtitles.DownloadSubtitles(downloadsList);
            if (!(resultDownLoad is MethodResponseSubtitleDownload))
            {
                _logger.Debug("invalid response type");
                return ItemUpdateType.None;
            }
            if (!((MethodResponseSubtitleDownload) resultDownLoad).Results.Any()) {
                _logger.Debug("No Subtitle Downloads");
                return ItemUpdateType.None;                
            }
            var res = ((MethodResponseSubtitleDownload) resultDownLoad).Results.First();
            var data = Convert.FromBase64String(res.Data);
            var target = Utilities.Decompress(new MemoryStream(data));
            // now save the subtitle
            var fileName = Path.Combine(fileInfo.DirectoryName, string.Format("{0}.{1}.srt", Path.GetFileNameWithoutExtension(fileInfo.Name), subtitle.SubLanguageID));

            Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            stream.Write(target, 0, target.Length);
            stream.Close();
            _logger.Debug("Subtitles downloaded: " + subtitle.SubFileName);
            var configuration = Plugin.Instance.Configuration;
            downloadedSubtitles.Add(new Subtitle {IdSubtitle = subtitle.IDSubtitle,Date = DateTime.Now,SubtitleFileName = subtitle.SubFileName});
            configuration.DownloadedSubtitles = downloadedSubtitles.ToArray();
            Plugin.Instance.UpdateConfiguration(configuration);
            return ItemUpdateType.MetadataEdit;
        }

        public string Name
        {
            get { return "Subtitle Provider"; }
        }

        public bool Supports(BaseItem item)
        {
            if (item == null || item.Path == null)
                return false;
            var users = _userManager.Users.Where(u =>
                                                 {
                                                     var user = UserHelper.GetUser(u);

                                                     return user != null && user.SubtitleLocations != null &&
                                                            user.SubtitleLocations.Length > 0;

                                                 }).ToList();
            if (users.All(x => UserHelper.GetUser(x).SubtitleLocations.All(l => item.Path != null && !item.Path.StartsWith(l + "\\"))))
                return false;
            var subtitles = Directory.GetFiles(Path.GetDirectoryName(item.Path), Path.GetFileNameWithoutExtension(item.Path)+"*.srt");
            if (subtitles.Length>0)
                return false;
            return item is Episode;
        }
    }
}