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

namespace RedditDownloaderAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DownloadController : ControllerBase
    {
        private readonly ILogger<DownloadController> _logger;

        public DownloadController(ILogger<DownloadController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            HttpRequest req = HttpContext.Request;

            string url = req.Query["url"];


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            url = url ?? data?.url;

            if (url == null)
            {
                return new BadRequestObjectResult("Please pass a name on the query string or in the request body");

            }

            string html = await GetHtmlFromUrl(url);
            string videoBaseUrl = await GetVideoBaseUrl(html);
            string videoUrl = videoBaseUrl + "/DASH_720.mp4";
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
