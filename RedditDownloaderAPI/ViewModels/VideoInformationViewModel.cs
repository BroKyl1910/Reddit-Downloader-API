using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedditDownloaderAPI.ViewModels
{
    public class VideoViewModel
    {
        public string VideoId { get; set; }
        public string VideoTitle { get; set; }
        public List<int> AvailableResolutions { get; set; }
        public string BaseDownloadUrl { get; set; }
        public string ThumbnailUrl { get; set; }

    }
}
