using System;
using System.Threading.Tasks;
using Networking.ITServices.Dto;
using RestSharp;

namespace Networking.ITServices;

internal sealed class CustomerBusinessService : ICustomerBusinessService
{
	private readonly ITRestClient _restClient;

	private readonly CustomerGuid? _customerGuid;

	private readonly ICache _cache;

	public CustomerBusinessService(CustomerBusinessRestClient restClient, ICache cache, CustomerGuid? customerGuid = null)
	{
		_restClient = restClient;
		_customerGuid = customerGuid;
		_cache = cache;
	}

	public async Task<Customer> GetCustomerAsync()
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		string text = $"/customerBusinessServices/rest/GarminCustomer/customers/{_customerGuid}";
		byte[] cacheKey = Hash.GetSha1(text);
		Customer customer = _cache.Get<Customer>(cacheKey, TimeSpan.FromDays(1.0));
		if ((object)customer != null)
		{
			return customer;
		}
		Customer customer2 = default(Customer);
		try
		{
			RestRequest request = new RestRequest(text);
			RestResponse<Customer> restResponse = await _restClient.ExecuteAsync<Customer>(request);
			_cache.Set(cacheKey, restResponse);
			return restResponse.Data;
		}
		catch when (((Func<bool>)delegate
		{
			// Could not convert BlockContainer to single expression
			customer2 = _cache.Get<Customer>(cacheKey, TimeSpan.FromDays(28.0));
			return (object)customer2 != null;
		}).Invoke())
		{
			return customer2;
		}
	}
}
