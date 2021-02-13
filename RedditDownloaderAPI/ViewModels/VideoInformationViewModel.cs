using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedditDownloaderAPI.ViewModels
{
    public abstract class MediaViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string BaseDownloadUrl { get; set; }
        public string ThumbnailUrl { get; set; }

        public MediaType MediaType { get; set; }
    }

    public enum MediaType
    {
        VIDEO,
        GIF
    }

    public class VideoViewModel : MediaViewModel
    {
        public List<int> AvailableResolutions { get; set; }
    }

    public class GifViewModel : MediaViewModel
    {
        public string Provider { get; set; }
    }
}
