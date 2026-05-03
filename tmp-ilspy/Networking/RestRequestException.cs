using System;
using System.Linq;
using System.Net;
using RestSharp;

namespace Networking;

public class RestRequestException : Exception
{
	public RestResponse Response { get; }

	public HttpStatusCode StatusCode => Response.StatusCode;

	public RestRequestException(RestResponse response)
		: base(response.ErrorMessage, response.ErrorException)
	{
		Response = response;
		Data["StatusCode"] = (int)response.StatusCode;
		string value = response.Headers?.FirstOrDefault((HeaderParameter i) => i.Name == "garmin-trace-id")?.Value?.ToString();
		if (!string.IsNullOrWhiteSpace(value))
		{
			Data["garmin-trace-id"] = value;
		}
	}
}
