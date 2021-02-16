using RedditDownloaderAPI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedditDownloaderAPI
{
    public class GifDownloader : IMediaDownloader
    {
        public Task<byte[]> DownloadMedia(string url, int quality)
        {
            throw new NotImplementedException();
        }

        public Task<MediaViewModel> GetMediaInformation(string url)
        {
            throw new NotImplementedException();
        }
    }
}
