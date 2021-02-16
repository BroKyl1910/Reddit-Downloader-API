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
}
