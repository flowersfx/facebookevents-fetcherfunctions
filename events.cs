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
			var url = $"https://graph.facebook.com/v3.2/{pageid}/events?event_state_filter=['published']&time_filter=upcoming&access_token={token}";
            var jsonreader = new JsonTextReader(new StringReader(await Get(url)));
            jsonreader.DateParseHandling = DateParseHandling.None;
            JArray fbevents = (JArray)JObject.Load( jsonreader )["data"];
            var events = fbevents.Select( o => new Event {
                eventname = (string)o["name"],
                eventdescription = (string)o["description"],
                eventplacename = (string)o["place"]["name"],
                eventplacestreet = (string)o["place"]["location"]["street"],
                eventstarttime = (string)o["start_time"],
                eventendtime = (string)o["end_time"]
            } ).OrderBy( o => o.eventstarttime ).ToArray();
            
			return new OkObjectResult( new Events { events = events } );
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
