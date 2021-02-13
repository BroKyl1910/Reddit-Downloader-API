using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp;
using FFMpegCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RedditDownloaderAPI.ViewModels;

namespace RedditDownloaderAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class VideoDownloadController : ControllerBase
    {
        private readonly ILogger<VideoDownloadController> _logger;

        private const string AUDIO_DIRECTORY = "AppData/Audio";
        private const string VIDEO_DIRECTORY = "AppData/Videos";
        private const string OUTPUT_DIRECTORY = "AppData/Outputs";
        private const string BACKUP_DIRECTORY = "AppData/_backups";

        public VideoDownloadController(ILogger<VideoDownloadController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<VideoViewModel> VideoInformation()
        {
            HttpRequest req = HttpContext.Request;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string url = data?.url;

            string html = await GetHtmlFromUrl(url);
            VideoViewModel videoViewModel = await ParseVideoInformation(html);

            return videoViewModel;
        }

        [HttpGet]
        public async Task<IActionResult> Download()
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            HttpRequest req = HttpContext.Request;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string baseUrl = data?.baseUrl;
            int quality = data?.quality ?? -1;

            if (baseUrl == null)
            {
                return new BadRequestObjectResult("Please pass a url in the request body");

            }

            if (quality == -1)
            {
                return new BadRequestObjectResult("Please pass a quality in the request body");

            }

            string videoUrl = baseUrl + "/DASH_" + quality + ".mp4";
            string audioUrl = baseUrl + "/DASH_audio.mp4";

            bool hasAudio = await UrlExists(new CustomWebClient() { Method = "HEAD" }, new Uri(audioUrl));

            long fileTime = DateTime.Now.ToFileTime();
            string videoFileName = VIDEO_DIRECTORY + "/video_" + fileTime + ".mp4";
            string audioFileName = AUDIO_DIRECTORY + "/audio_" + fileTime + ".mp4";
            string outputFileName = OUTPUT_DIRECTORY + "/output_" + fileTime + ".mp4";
            string backupFileName = BACKUP_DIRECTORY + "/backup_" + fileTime + ".mp4";

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
                    VIDEO_DIRECTORY,
                    AUDIO_DIRECTORY,
                    OUTPUT_DIRECTORY
                });

            return (ActionResult)new OkObjectResult(fileBytes);
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
                    if (await UrlExists(webClient, new Uri(videoUrl)))
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

        private async Task<bool> UrlExists(WebClient webClient, Uri uri)
        {
            try
            {
                await webClient.DownloadStringTaskAsync(uri);
            }
            catch (WebException ex)
            {
                return false;
            }

            return true;
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

        private static async Task<string> GetHtmlFromUrl(string fullUrl)
        {
            HttpClient client = new HttpClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var response = client.GetStringAsync(fullUrl);
            return await response;
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
    }
}
