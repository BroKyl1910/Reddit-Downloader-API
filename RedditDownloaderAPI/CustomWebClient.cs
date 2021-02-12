using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RedditDownloaderAPI
{
    // Custom class to allow HEAD method request to check if urls are valid
    public class CustomWebClient : WebClient
    {
        public string Method { get; set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest webRequest = base.GetWebRequest(address);

            if (!string.IsNullOrEmpty(Method))
                webRequest.Method = Method;

            return webRequest;
        }
    }
}
