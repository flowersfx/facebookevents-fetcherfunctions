using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Mandagsklubben.Events
{
    public static class events
    {
        static TimeSpan BlobTimeout = TimeSpan.FromMinutes(60);

        [FunctionName("events")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            string storageConnectionString = config["BLOB_STORAGE_CONNECTION_STRING"];
            var account = CloudStorageAccount.Parse(storageConnectionString);

            CloudBlobClient cloudBlobClient = account.CreateCloudBlobClient();
            var events = await DownloadBlobString(cloudBlobClient);
            if( DateTime.Parse(events.date).IsOutDated() )
            {
                events = await GetFacebookEvents(config);
                await UploadBlobString(cloudBlobClient,events);
            }
			return new OkObjectResult(events);
        }

        public static bool IsOutDated( this DateTime blobdate )
        {
            return blobdate + BlobTimeout < DateTime.UtcNow;
        }

        public static async Task<Events> GetFacebookEvents(IConfigurationRoot config )
        {
            var pageid = config["FACEBOOK_PAGE_ID"];
			var token = config["FACEBOOK_PAGE_ACCESS_TOKEN"];
			var url = $"https://graph.facebook.com/{pageid}/events?time_filter=upcoming&fields=cover,name,description,place,start_time,end_time&access_token={token}";
            var jsonreader = new JsonTextReader(new StringReader(await Get(url)));
            jsonreader.DateParseHandling = DateParseHandling.None;
            JArray fbevents = (JArray)JObject.Load(jsonreader)["data"];
            var events = new List<Event>();

            foreach(var fbevent in fbevents) {
                var revent = new Event();
                revent.id = (fbevent["id"] ?? string.Empty).ToString();
                revent.name = (fbevent["name"] ?? string.Empty).ToString();
                revent.description = (fbevent["description"] ?? string.Empty).ToString();
                
                var fbplace = fbevent["place"]; // inlining is for machines
                if (fbplace != null)
                {
                    revent.placename = (fbplace["name"] ?? string.Empty).ToString();
                    var fblocation = fbplace["location"];
                    if (fblocation != null)
                    {
                        revent.placestreet = (fblocation["street"] ?? string.Empty).ToString();
                    }
                } else {
                    revent.placename = string.Empty;
                    revent.placestreet = string.Empty;
                }

                revent.starttime = (fbevent["start_time"] ?? string.Empty).ToString();
                revent.endtime = (fbevent["end_time"] ?? string.Empty).ToString();
                var fbcover = fbevent["cover"];

                if (fbcover != null)
                {
                    revent.coverurl = (fbcover["source"] ?? string.Empty).ToString();
                } 
                else
                {
                    revent.coverurl = string.Empty;
                }

                events.Add(revent);
            }
            return new Events {
                events = events.OrderBy(t => t.starttime ).ToArray(),
                date = DateTime.UtcNow.ToString("s")
            };
        }

        public static async Task<Events> DownloadBlobString(CloudBlobClient storageClient)
        {
            var cloudBlobContainer = storageClient.GetContainerReference("mandagsklubben-events");
            CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference("events.json");
            var jsonstr = await cloudBlockBlob.DownloadTextAsync();
            return JsonConvert.DeserializeObject<Events>(jsonstr);
        }

        public static async Task UploadBlobString(CloudBlobClient storageClient, Events events)
        {
            var cloudBlobContainer = storageClient.GetContainerReference("mandagsklubben-events");
            CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference("events.json");
            var jsonstr = JsonConvert.SerializeObject(events);
            await cloudBlockBlob.UploadTextAsync(jsonstr);
        }

        public static async Task<string> Get(string url)
        {
            var request = System.Net.WebRequest.Create(url);
            using( var response = await request.GetResponseAsync() )
            using( var reader = new StreamReader( response.GetResponseStream() ) )
            {
                return reader.ReadToEnd();
            }
        }
    }

    public class Events
    {
        public Event[] events { get; set; }
        public string date { get; set; }
    }

    public class Event
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string coverurl { get; set; }
        public string placename { get; set; }
        public string placestreet { get; set; }
        public string starttime { get; set; }
        public string endtime { get; set; }
        public string coverwidth { get; set; }
        public string coverheight { get; set; }
    }
}
