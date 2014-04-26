using System;
using System.Linq;
using MediaBrowser.SubtitleDownloader.Model;

namespace MediaBrowser.SubtitleDownloader.Helper
{
    internal static class UserHelper
    {
        public static User GetUser(Controller.Entities.User user)
        {
            return Plugin.Instance.PluginConfiguration.Users != null ? Plugin.Instance.PluginConfiguration.Users.FirstOrDefault(tUser => new Guid(tUser.LinkedMbUserId).Equals(user.Id)) : null;
        }

        public static User GetUser(string userId)
        {
            var userGuid = new Guid(userId);
            return Plugin.Instance.PluginConfiguration.Users != null ? Plugin.Instance.PluginConfiguration.Users.FirstOrDefault(tUser => new Guid(tUser.LinkedMbUserId).Equals(userGuid)) : null;
        }
    }
}