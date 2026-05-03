using System.Collections.Generic;
using System.Threading.Tasks;
using Networking.InternalOmt.Dto;
using RestSharp;

namespace Networking.InternalOmt;

internal sealed class Releases : IReleases
{
	private readonly InternalOmtRestClient _restClient;

	public Releases(InternalOmtRestClient restClient)
	{
		_restClient = restClient;
	}

	public async Task<Release> GetReleaseByIdentifierAsync(Identifier releaseIdentifier)
	{
		releaseIdentifier.Deconstruct(out string ProductGroupCode, out int MajorVersion, out int MinorVersion);
		string arg = ProductGroupCode;
		int num = MajorVersion;
		int num2 = MinorVersion;
		RestRequest request = new RestRequest($"/rce/internal/releases/{arg}-{num}-{num2}");
		return (await _restClient.ExecuteAsync<Release>(request)).Data;
	}

	public async Task<List<Release>> GetReleasesByProductGroupAsync(string productGroupCode, bool activeOnly)
	{
		RestRequest request = new RestRequest("/rce/internal/productgroups/" + productGroupCode + "/releases");
		request.AddParameter("activeOnly", activeOnly);
		return (await _restClient.ExecuteAsync<List<Release>>(request)).Data;
	}
}
