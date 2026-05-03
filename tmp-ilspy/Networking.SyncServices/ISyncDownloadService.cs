using System;
using System.Threading.Tasks;
using RestSharp;

namespace Networking.SyncServices;

public interface ISyncDownloadService
{
	Task<RestResponse> DownloadFileAsync(Uri uri);
}
