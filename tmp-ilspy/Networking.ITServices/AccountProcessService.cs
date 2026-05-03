using System;
using System.Threading.Tasks;
using RestSharp;

namespace Networking.ITServices;

internal sealed class AccountProcessService : IAccountProcessService
{
	private readonly ITRestClient _restClient;

	private readonly Guid _customerGuid;

	public AccountProcessService(AccountProcessRestClient restClient, CustomerGuid customerGuid)
	{
		_restClient = restClient;
		_customerGuid = customerGuid.Guid;
	}

	public async Task<string> CheckPasswordAsync(string password)
	{
		RestRequest request = new CustomLoggingRestRequest($"/accountProcessServices/customerAccounts/{_customerGuid}/checkPasswordForToken", Method.Post);
		request.AddHeader("Content-Type", "application/json");
		request.AddHeader("Accept", "application/json");
		request.AddParameter(string.Empty, password, ParameterType.RequestBody);
		RestResponse restResponse = await _restClient.ExecuteAsync(request);
		return string.Equals(restResponse.Content.ToLowerInvariant(), "false") ? string.Empty : restResponse.Content;
	}
}
