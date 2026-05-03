using System.Collections.Generic;
using System.Threading.Tasks;
using Networking.InternalOmt.Dto;
using RestSharp;

namespace Networking.InternalOmt;

internal sealed class Regions : IRegions
{
	private readonly InternalOmtRestClient _restClient;

	public Regions(InternalOmtRestClient restClient)
	{
		_restClient = restClient;
	}

	public async Task<Region> GetRegionByRegionPartNumberAsync(string regionPartNumber)
	{
		RestRequest request = new RestRequest("/rce/internal/regions/" + regionPartNumber);
		return (await _restClient.ExecuteAsync<Region>(request)).Data;
	}

	public async Task<List<Region>> GetRegionsByContentAsync(string contentPartNumber)
	{
		RestRequest request = new RestRequest("/rce/internal/contents/" + contentPartNumber + "/regions");
		return (await _restClient.ExecuteAsync<List<Region>>(request)).Data;
	}

	public async Task<List<Region>> GetRegionsByReleaseAsync(Identifier releaseIdentifier)
	{
		releaseIdentifier.Deconstruct(out string ProductGroupCode, out int MajorVersion, out int MinorVersion);
		string arg = ProductGroupCode;
		int num = MajorVersion;
		int num2 = MinorVersion;
		RestRequest request = new RestRequest($"/rce/internal/releases/{arg}-{num}-{num2}/regions");
		return (await _restClient.ExecuteAsync<List<Region>>(request)).Data;
	}
}
