using System;
using System.Net;

namespace Networking.DownloadManager;

public class DownloadManagerException : Exception
{
	public string DownloadUrl { get; }

	public HttpStatusCode StatusCode { get; }

	internal DownloadManagerException(string downloadUrl, HttpStatusCode statusCode)
	{
		DownloadUrl = downloadUrl;
		StatusCode = statusCode;
	}
}
