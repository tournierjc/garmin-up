using System.Net;
using System.Net.Http;

namespace Networking;

public class HttpRequestManagerException : HttpRequestException
{
	public new HttpStatusCode StatusCode { get; }

	public HttpRequestManagerException(HttpStatusCode statusCode)
		: base(statusCode.ToString())
	{
		StatusCode = statusCode;
	}
}
