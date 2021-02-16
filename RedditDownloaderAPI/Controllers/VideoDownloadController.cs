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
using RedditDownloaderAPI.Helpers;
using RedditDownloaderAPI.ViewModels;

namespace RedditDownloaderAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class VideoDownloadController : ControllerBase
    {
        private readonly ILogger<VideoDownloadController> _logger;

        VideoDownloader videoDownloader = new VideoDownloader();
        GifDownloader gifDownloader = new GifDownloader();

        public VideoDownloadController(ILogger<VideoDownloadController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<VideoViewModel> Information(string url)
        {
            VideoViewModel viewModel = (VideoViewModel) await videoDownloader.GetMediaInformation(url);
            return viewModel;
        }

        [HttpGet]
        public async Task<IActionResult> Download(string baseUrl, int quality=-1)
        {
            if (baseUrl == null)
            {
                return new BadRequestObjectResult("Please pass a url in the request body");

            }

            if (quality == -1)
            {
                return new BadRequestObjectResult("Please pass a quality in the request body");

            }

            byte[] fileBytes = await videoDownloader.DownloadMedia(baseUrl, quality);

            return (ActionResult)new OkObjectResult(fileBytes);
        }

        
    }
}
