using System.Collections.Generic;

namespace RedditDownloaderAPI.ViewModels
{
    public class VideoViewModel : MediaViewModel
    {
        public List<int> AvailableResolutions { get; set; }
    }
}
