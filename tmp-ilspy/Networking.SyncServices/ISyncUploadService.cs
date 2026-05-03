using System;
using System.Threading.Tasks;
using Networking.SyncServices.Dto;
using RestSharp;

namespace Networking.SyncServices;

public interface ISyncUploadService
{
	IRestClient GetSyncRestClient();

	Task<UploadResponse?> UploadItemAsync(AuthType authType, UploadType uploadType, Uri uploadUri, byte[] fileToUpload, bool isLastFile);
}
