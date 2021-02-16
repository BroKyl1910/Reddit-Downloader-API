using AngleSharp;
using FFMpegCore;
using RedditDownloaderAPI.Helpers;
using RedditDownloaderAPI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RedditDownloaderAPI
{
    public class VideoDownloader : IMediaDownloader
    {
        public async Task<MediaViewModel> GetMediaInformation(string url)
        {
            string html = await GetHtmlFromUrl(url);
            VideoViewModel videoViewModel = await ParseVideoInformation(html);

            return videoViewModel;
        }
        public async Task<byte[]> DownloadMedia(string baseUrl, int quality)
        {
            string videoUrl = baseUrl + "/DASH_" + quality + ".mp4";
            string audioUrl = baseUrl + "/DASH_audio.mp4";

            bool hasAudio = await UrlHelper.UrlExists(new CustomWebClient() { Method = "HEAD" }, new Uri(audioUrl));

            long fileTime = DateTime.Now.ToFileTime();
            string videoFileName = UrlHelper.VIDEO_DIRECTORY + "/video_" + fileTime + ".mp4";
            string audioFileName = UrlHelper.AUDIO_DIRECTORY + "/audio_" + fileTime + ".mp4";
            string outputFileName = UrlHelper.OUTPUT_DIRECTORY + "/output_" + fileTime + ".mp4";
            string backupFileName = UrlHelper.BACKUP_DIRECTORY + "/backup_" + fileTime + ".mp4";

            if (hasAudio)
            {
                await downloadFile(videoUrl, videoFileName);
                await downloadFile(audioUrl, audioFileName);


                combineVideoAudio(videoFileName, audioFileName, outputFileName);
            }
            else
            {
                await downloadFile(videoUrl, outputFileName);
            }

            BackupOutput(outputFileName, backupFileName);
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(outputFileName);

            ClearDirectories(new List<string> {
                    UrlHelper.VIDEO_DIRECTORY,
                    UrlHelper.AUDIO_DIRECTORY,
                    UrlHelper.OUTPUT_DIRECTORY
                });

            VideoDownloadViewModel videoDownloadViewModel = new VideoDownloadViewModel()
            {
                FileBytes = fileBytes,
                Size = fileBytes.Length,
                Title = outputFileName
            };

            return fileBytes;
        }

        private async Task<VideoViewModel> ParseVideoInformation(string html)
        {
            VideoViewModel videoViewModel = new VideoViewModel() { MediaType = MediaType.VIDEO };
            var config = Configuration.Default;

            //Create a new context for evaluating webpages with the given config
            var context = BrowsingContext.New(config);

            //Just get the DOM representation
            var document = await context.OpenAsync(req => req.Content(html));

            // Get video url from source
            var sourceAttr = document.QuerySelector("source").GetAttribute("src");
            var sourceParts = sourceAttr.Split('/');
            var videoId = sourceParts[3];

            // Get video title
            var title = document.QuerySelector("title").InnerHtml;

            var videoBaseUrl = "https://v.redd.it/" + videoId;
            List<int> availableResolutions = await GetAvailableResolutions(videoBaseUrl);

            string thumbnailUrl = GetThumbnailUrl(html);

            videoViewModel.Id = videoId;
            videoViewModel.Title = title;
            videoViewModel.BaseDownloadUrl = videoBaseUrl;
            videoViewModel.AvailableResolutions = availableResolutions;
            videoViewModel.ThumbnailUrl = thumbnailUrl;

            return videoViewModel;
        }

        private static async Task<string> GetHtmlFromUrl(string fullUrl)
        {
            HttpClient client = new HttpClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var response = client.GetStringAsync(fullUrl);
            return await response;
        }

        private string GetThumbnailUrl(string html)
        {
            string query = "\"thumbnail\":{\"url\":";

            // searching for content after query, plus 1 because json "
            int startIndex = html.IndexOf(query) + query.Length + 1;

            int endIndex = html.IndexOf("\"", startIndex);

            string url = html.Substring(startIndex, (endIndex - startIndex));

            return url;
        }

        private async Task<List<int>> GetAvailableResolutions(string videoBaseUrl)
        {
            var resolutions = new List<int>() {
                240,
                360,
                480,
                720,
                1080
            };

            var availableResolutions = new List<int>();

            using (CustomWebClient webClient = new CustomWebClient() { Method = "HEAD" })
            {

                foreach (var res in resolutions)
                {
                    string videoUrl = videoBaseUrl + "/DASH_" + res + ".mp4";
                    if (await UrlHelper.UrlExists(webClient, new Uri(videoUrl)))
                    {
                        availableResolutions.Add(res);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return availableResolutions;
        }

        private static async Task<string> GetVideoBaseUrl(string html)
        {
            var config = Configuration.Default;

            //Create a new context for evaluating webpages with the given config
            var context = BrowsingContext.New(config);

            //Just get the DOM representation
            var document = await context.OpenAsync(req => req.Content(html));

            //Video source is in <source src="..">
            var sourceElement = document.QuerySelector("source");
            string sourceAttr = sourceElement.GetAttribute("src");

            // Get video url from source
            var sourceParts = sourceAttr.Split('/');
            var videoId = sourceParts[3];

            var videoBaseUrl = "https://v.redd.it/" + videoId;
            return videoBaseUrl;

        }

        private void BackupOutput(string outputFileName, string backupFileName)
        {
            System.IO.File.Copy(outputFileName, backupFileName, true);
        }

        private void ClearDirectories(List<string> directories)
        {
            foreach (string dir in directories)
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(dir);

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }

            }
        }


        private void combineVideoAudio(string videoFileName, string audioFileName, string outputFileName)
        {
            FFMpeg.ReplaceAudio(videoFileName, audioFileName, outputFileName, true);
        }

        private async Task downloadFile(string fileUrl, string fileName)
        {
            WebClient webClient = new WebClient();
            await webClient.DownloadFileTaskAsync(fileUrl, fileName);
        }
    }
}
