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

namespace Mandagsklubben.Events
{
    public static class events
    {
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

            var pageid = config["FACEBOOK_PAGE_ID"];
			var token = config["FACEBOOK_PAGE_ACCESS_TOKEN"];
			var url = $"https://graph.facebook.com/{pageid}/events?time_filter=upcoming&fields=cover,name,description,place,start_time,end_time&access_token={token}";
            var jsonreader = new JsonTextReader(new StringReader(await Get(url)));
            jsonreader.DateParseHandling = DateParseHandling.None;
            JArray fbevents = (JArray)JObject.Load(jsonreader)["data"];
            var events = new List<Event>();

            foreach(var fbevent in fbevents) {
                var revent = new Event();
                revent.eventname = (fbevent["name"] ?? string.Empty).ToString();
                revent.eventdescription = (fbevent["description"] ?? string.Empty).ToString();
                
                var fbplace = fbevent["place"]; // inlining is for machines
                if (fbplace != null)
                {
                    revent.eventplacename = (fbplace["source"] ?? string.Empty).ToString();
                    var fblocation = fbplace["location"];
                    if (fblocation != null)
                    {
                        revent.eventplacestreet = (fblocation["street"] ?? string.Empty).ToString();
                    }
                } else {
                    revent.eventplacename = string.Empty;
                    revent.eventplacestreet = string.Empty;
                }

                revent.eventstarttime = (fbevent["start_time"] ?? string.Empty).ToString();
                revent.eventendtime = (fbevent["end_time"] ?? string.Empty).ToString();
                var fbcover = fbevent["cover"];

                if (fbcover != null)
                {
                    revent.eventcoverurl = (fbcover["source"] ?? string.Empty).ToString();
                } 
                else
                {
                    revent.eventcoverurl = string.Empty;
                }

                events.Add(revent);
            }
            
			return new OkObjectResult( new Events { events = events.OrderBy(t => t.eventstarttime ).ToArray() } );
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
    }

    public class Event
    {
        public string eventname { get; set; }
        public string eventdescription { get; set; }
        public string eventcoverurl { get; set; }
        public string eventplacename { get; set; }
        public string eventplacestreet { get; set; }
        public string eventstarttime { get; set; }
        public string eventendtime { get; set; }
    }
}
