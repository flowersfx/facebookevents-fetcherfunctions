#r "Newtonsoft.Json"
#r "System.Web"

using System.Net;
using System.Web;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

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

public static async Task<string> Get( string url )
{
	var request = WebRequest.Create(url);
	using( var response = await request.GetResponseAsync() )
	using( var reader = new StreamReader( response.GetResponseStream() ) )
	{
		return reader.ReadToEnd();
	}
}

public static async Task<HttpResponseMessage> Run( HttpRequestMessage req, TraceWriter log )
{
	try
	{

		var token = System.Environment.GetEnvironmentVariable( "FACEBOOK_PAGE_ACCESS_TOKEN ", EnvironmentVariableTarget.Process );
		var url = "http://facebook.com"
		var data = await Get( url )
		var responsestr = JsonConvert.SerializeObject( new Event{eventname="test", eventdescription = data} );

		if( rssstr == null )
			return req.CreateResponse( HttpStatusCode.NotFound );

		return req.CreateResponse( HttpStatusCode.OK, responsestr, "text/plain" );
	}
	catch( Exception e )
	{
		return req.CreateResponse( HttpStatusCode.BadRequest, e.ToString(), "text/plain" );
	}
}
