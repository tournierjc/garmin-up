using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Networking.Auth;
using RestSharp;
using RestSharp.Authenticators;

namespace Networking.ITServices;

internal class ITRestClient : NetworkingRestClient, IAuthenticator
{
	private readonly IAuthDataProvider _auth;

	private readonly ITAuth? _itAuth;

	private readonly UnitId? _unitId;

	private readonly CustomerGuid? _customerGuid;

	public ITRestClient(ILogger<ITRestClient> logger, GarminEnvironment garminEnvironment, ClientId clientId, IAuthDataProvider auth, ITAuth? itAuth = null, UnitId? unitId = null, CustomerGuid? customerGuid = null)
		: base(logger, GetBaseUrl(garminEnvironment))
	{
		_auth = auth;
		_itAuth = itAuth;
		_unitId = unitId;
		_customerGuid = customerGuid;
		AddDefaultHeader("x-garmin-client-id", clientId.Id);
	}

	public ITRestClient(ILogger<ITRestClient> logger, GarminEnvironment garminEnvironment, ClientId clientId, IAuthDataProvider auth, string baseUrl, ITAuth? itAuth = null, UnitId? unitId = null, CustomerGuid? customerGuid = null)
		: base(logger, baseUrl)
	{
		_auth = auth;
		_itAuth = itAuth;
		_unitId = unitId;
		_customerGuid = customerGuid;
		AddDefaultHeader("x-garmin-client-id", clientId.Id);
	}

	public static string GetBaseUrl(GarminEnvironment garminEnvironment)
	{
		switch (garminEnvironment)
		{
		case GarminEnvironment.Production:
		case GarminEnvironment.Demo:
			return "https://services.garmin.com/";
		case GarminEnvironment.Stage:
			return "https://servicesstg.garmin.com/";
		case GarminEnvironment.Test:
		case GarminEnvironment.ChinaTest:
			return "https://servicestest.garmin.com/";
		case GarminEnvironment.China:
			return "https://services.garmin.cn/";
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public override async Task<RestResponse> ExecuteAsync(RestRequest request, CancellationToken token = default(CancellationToken))
	{
		RestResponse result = default(RestResponse);
		int num;
		try
		{
			result = await base.ExecuteAsync(request, token);
			return result;
		}
		catch (RestRequestException ex) when (_customerGuid != null && ex.StatusCode == HttpStatusCode.Unauthorized)
		{
			num = 1;
		}
		if (num != 1)
		{
			return result;
		}
		_auth.ExpireITAuth(_customerGuid);
		return await base.ExecuteAsync(request, token);
	}

	public override async Task<RestResponse<T>> ExecuteAsync<T>(RestRequest request, CancellationToken token = default(CancellationToken))
	{
		RestResponse<T> result = default(RestResponse<T>);
		int num;
		try
		{
			result = await base.ExecuteAsync<T>(request, token);
			return result;
		}
		catch (RestRequestException ex) when (_customerGuid != null && ex.StatusCode == HttpStatusCode.Unauthorized)
		{
			num = 1;
		}
		if (num != 1)
		{
			return result;
		}
		_auth.ExpireITAuth(_customerGuid);
		return await base.ExecuteAsync<T>(request, token);
	}

	async ValueTask IAuthenticator.Authenticate(IRestClient client, RestRequest request)
	{
		if (_itAuth != null)
		{
			request.AddOrUpdateHeader("Authorization", "Bearer " + _itAuth.AccessToken);
			return;
		}
		bool flag = _unitId != null;
		ITAuth iTAuth = default(ITAuth);
		if (flag)
		{
			iTAuth = await _auth.GetITAuthAsync(_unitId);
			flag = (object)iTAuth != null;
		}
		if (flag)
		{
			request.AddOrUpdateHeader("Authorization", "Bearer " + iTAuth.AccessToken);
			return;
		}
		flag = _customerGuid != null;
		ITAuth iTAuth2 = default(ITAuth);
		if (flag)
		{
			iTAuth2 = await _auth.GetITAuthAsync(_customerGuid);
			flag = (object)iTAuth2 != null;
		}
		if (flag)
		{
			request.AddOrUpdateHeader("Authorization", "Bearer " + iTAuth2.AccessToken);
		}
		else if (_unitId != null || _customerGuid != null)
		{
			throw new ITAuthorizationException();
		}
	}
}
