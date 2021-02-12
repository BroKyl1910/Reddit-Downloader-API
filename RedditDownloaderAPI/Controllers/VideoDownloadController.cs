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
            string url = data?.url;
            int quality = data?.quality ?? -1;

            if (url == null)
            {
                return new BadRequestObjectResult("Please pass a url in the request body");

            }

            if (quality == -1)
            {
                return new BadRequestObjectResult("Please pass a quality in the request body");

            }

            string html = await GetHtmlFromUrl(url);
            string videoBaseUrl = await GetVideoBaseUrl(html);
            string videoUrl = videoBaseUrl + "/DASH_" + quality + ".mp4";
            string audioUrl = videoBaseUrl + "/DASH_audio.mp4";

            long fileTime = DateTime.Now.ToFileTime();
            string videoFileName = "AppData/Videos/video - " + fileTime + ".mp4";
            string audioFileName = "AppData/Audio/audio - " + fileTime + ".mp4";

            await downloadFile(videoUrl, videoFileName);
            await downloadFile(audioUrl, audioFileName);

            string outputFileName = "AppData/Outputs/output - " + fileTime + ".mp4";

            combineVideoAudio(videoFileName, audioFileName, outputFileName);

            return (ActionResult)new OkObjectResult($"Html: {html}");
        }

        private async Task<VideoViewModel> ParseVideoInformation(string html)
        {
            VideoViewModel videoViewModel = new VideoViewModel();
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

            videoViewModel.VideoId = videoId;
            videoViewModel.VideoTitle = title;
            videoViewModel.BaseDownloadUrl = videoBaseUrl;
            videoViewModel.AvailableResolutions = availableResolutions;
            videoViewModel.ThumbnailUrl = thumbnailUrl;

            return videoViewModel;
        }

        private string GetThumbnailUrl(string html)
        {
            string query = "\"thumbnail\":{\"url\":";

            // searching for content after query, plus 1 because json "
            int startIndex = html.IndexOf(query)+query.Length + 1;

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
