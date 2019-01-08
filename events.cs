using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

            var idtest = config["FACEBOOK_PAGE_ID"];
            return new OkObjectResult(idtest);

            // try
            // {
            //     var token = ConfigurationManager.AppSettings["FACEBOOK_PAGE_ACCESS_TOKEN"];
            //     var pageid = ConfigurationManager.AppSettings["FACEBOOK_PAGE_ID"];
            //     var url = $"https://graph.facebook.com/{pageid}/";
            //     var data = await Get(url);
            //     var responsestr = JsonConvert.SerializeObject( new Event{eventname="test", eventdescription = data} );

            //     if (responsestr == null)
            //     {
            //         return req.CreateResponse( HttpStatusCode.NotFound );
            //     }

            //     return req.CreateResponse( HttpStatusCode.OK, responsestr, "text/plain" );
            // }
            // catch (Exception e)
            // {
            //     return req.CreateResponse( HttpStatusCode.BadRequest, e.ToString(), "text/plain" );
            // }
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
