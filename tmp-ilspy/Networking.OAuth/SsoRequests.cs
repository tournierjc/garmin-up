using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Networking.Auth;
using RestSharp;
using RestSharp.Interceptors;

namespace Networking.OAuth;

internal sealed class SsoRequests : ISsoRequests
{
	private record SsoTokenResponse(string LoginToken, string Username, string Service);

	private readonly SsoRestClient _restClient;

	private readonly string _ssoAppId;

	private readonly IAuthDataProvider _authProvider;

	private readonly UnitId? _unitId;

	private readonly CustomerGuid? _customerGuid;

	public SsoRequests(SsoRestClient restClient, SsoAppId ssoAppId, IAuthDataProvider authProvider, UnitId? unitId = null, CustomerGuid? customerGuid = null)
	{
		_restClient = restClient;
		_ssoAppId = ssoAppId.Id;
		_authProvider = authProvider;
		_unitId = unitId;
		_customerGuid = customerGuid;
	}

	public async Task<Uri> GetAutoLoginUrlAsync(string url)
	{
		ITAuth iTAuth = null;
		if (_unitId != null)
		{
			iTAuth = await _authProvider.GetITAuthAsync(_unitId);
		}
		if (iTAuth == null && _customerGuid != null)
		{
			iTAuth = await _authProvider.GetITAuthAsync(_customerGuid);
		}
		if (iTAuth == null)
		{
			throw new ITAuthorizationException();
		}
		RestRequest restRequest = new RestRequest("sso/requestToken", Method.Post);
		restRequest.AddParameter("accesstoken", iTAuth.AccessToken);
		restRequest.AddParameter("customerGUID", iTAuth.CustomerId);
		restRequest.AddParameter("appid", _ssoAppId);
		restRequest.AddParameter("service", new Uri(url).AbsoluteUri);
		restRequest.AddParameter("version", "4");
		CompatibilityInterceptor compatibilityInterceptor = new CompatibilityInterceptor();
		compatibilityInterceptor.OnBeforeDeserialization = delegate(RestResponse i)
		{
			i.ContentType = "application/json";
		};
		restRequest.Interceptors = new List<Interceptor> { compatibilityInterceptor };
		RestResponse<SsoTokenResponse> restResponse = await _restClient.ExecuteAsync<SsoTokenResponse>(restRequest);
		RestRequest request = new RestRequest("sso/login");
		request.AddParameter("logintoken", restResponse.Data.LoginToken);
		request.AddParameter("service", restResponse.Data.Service);
		request.AddParameter("generateExtraServiceTicket", "true");
		request.AddParameter("reauth", "true");
		return _restClient.BuildUri(request);
	}
}
