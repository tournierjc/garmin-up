using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeviceXmlUtil;
using Networking.InternalOmt.Dto;
using RestSharp;

namespace Networking.InternalOmt;

internal sealed class Contents : IContents
{
	private readonly InternalOmtRestClient _restClient;

	private readonly Func<XmlDevice> _xmlDeviceFunc;

	public Contents(InternalOmtRestClient restClient, Func<XmlDevice> xmlDeviceFunc)
	{
		_restClient = restClient;
		_xmlDeviceFunc = xmlDeviceFunc;
	}

	public async Task<Content> GetContentAsync(string contentPartNumber)
	{
		RestRequest request = new RestRequest("/rce/internal/contents/" + contentPartNumber);
		return (await _restClient.ExecuteAsync<Content>(request)).Data;
	}

	public async Task<List<Content>> GetContentAdditionsAsync(string contentPartNumber, string? productGroupCode)
	{
		RestRequest request = new RestRequest("/rce/internal/contents/" + contentPartNumber + "/additions");
		if (productGroupCode != null)
		{
			request.AddParameter("productGroupCode", productGroupCode);
		}
		return (await _restClient.ExecuteAsync<List<Content>>(request)).Data;
	}

	public async Task<List<Content>> GetContentsByReleaseAsync(Identifier releaseIdentifier)
	{
		releaseIdentifier.Deconstruct(out string ProductGroupCode, out int MajorVersion, out int MinorVersion);
		string arg = ProductGroupCode;
		int num = MajorVersion;
		int num2 = MinorVersion;
		RestRequest request = new RestRequest($"/rce/internal/releases/{arg}-{num}-{num2}/contents");
		return (await _restClient.ExecuteAsync<List<Content>>(request)).Data;
	}

	public async Task<List<string>> GetContentsOnDeviceAsync()
	{
		RestRequest request = new RestRequest("/rce/internal/device/contents", Method.Post);
		request.AddParameter("GarminDeviceXml", _xmlDeviceFunc().ToString());
		return (await _restClient.ExecuteAsync<List<string>>(request)).Data;
	}
}
