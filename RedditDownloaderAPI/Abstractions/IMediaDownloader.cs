using RedditDownloaderAPI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedditDownloaderAPI
{
    interface IMediaDownloader
    {
        public Task<MediaViewModel> GetMediaInformation(string url);
        public Task<byte[]> DownloadMedia(string url, int quality);
    }
}
