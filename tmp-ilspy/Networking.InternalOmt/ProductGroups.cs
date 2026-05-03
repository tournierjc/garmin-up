using System.Collections.Generic;
using System.Threading.Tasks;
using Networking.InternalOmt.Dto;
using RestSharp;

namespace Networking.InternalOmt;

internal sealed class ProductGroups : IProductGroups
{
	private readonly InternalOmtRestClient _restClient;

	public ProductGroups(InternalOmtRestClient restClient)
	{
		_restClient = restClient;
	}

	public async Task<List<ProductGroup>> GetProductGroupsAsync()
	{
		RestRequest request = new RestRequest("/rce/internal/productgroups");
		return (await _restClient.ExecuteAsync<List<ProductGroup>>(request)).Data;
	}

	public async Task<ProductGroup> GetProductGroupByGroupCodeAsync(string groupCode)
	{
		RestRequest request = new RestRequest("/rce/internal/productgroups/" + groupCode);
		return (await _restClient.ExecuteAsync<ProductGroup>(request)).Data;
	}
}
