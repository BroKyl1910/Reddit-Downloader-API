using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RedditDownloaderAPI.Helpers
{
    public class UrlHelper
    {
        public const string AUDIO_DIRECTORY = "AppData/Audio";
        public const string VIDEO_DIRECTORY = "AppData/Videos";
        public const string OUTPUT_DIRECTORY = "AppData/Outputs";
        public const string BACKUP_DIRECTORY = "AppData/_backups";


        public static async Task<bool> UrlExists(WebClient webClient, Uri uri)
        {
            try
            {
                await webClient.DownloadStringTaskAsync(uri);
            }
            catch (WebException)
            {
                return false;
            }

            return true;
        }
    }
}
