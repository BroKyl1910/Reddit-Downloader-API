using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedditDownloaderAPI.ViewModels
{
    public class VideoDownloadViewModel
    {
        public string Title { get; set; }
        public long Size { get; set; }
        public byte[] FileBytes { get; set; }
    }
}
