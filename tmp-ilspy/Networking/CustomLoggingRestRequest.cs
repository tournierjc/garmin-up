using System;
using RestSharp;

namespace Networking;

public class CustomLoggingRestRequest : RestRequest
{
	public bool CensorRequest { get; }

	public bool CensorResponse { get; }

	public CustomLoggingRestRequest(string resource, Method method, bool censorRequest = true, bool censorResponse = true)
		: base(resource, method)
	{
		CensorRequest = censorRequest;
		CensorResponse = censorResponse;
	}

	public CustomLoggingRestRequest(Uri resource, Method method, bool censorRequest = true, bool censorResponse = true)
		: base(resource, method)
	{
		CensorRequest = censorRequest;
		CensorResponse = censorResponse;
	}
}
