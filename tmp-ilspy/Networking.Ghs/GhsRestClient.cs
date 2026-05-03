using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Networking.Auth;
using RestSharp;
using RestSharp.Authenticators;

namespace Networking.Ghs;

internal sealed class GhsRestClient : NetworkingRestClient, IAuthenticator
{
	private readonly IAuthDataProvider _auth;

	private readonly UnitId? _unitId;

	public GhsRestClient(ILogger<GhsRestClient> logger, GarminEnvironment garminEnvironment, IAuthDataProvider auth, UnitId? unitId = null)
		: base(logger, GetBaseUrl(garminEnvironment))
	{
		_auth = auth;
		_unitId = unitId;
	}

	private static string GetBaseUrl(GarminEnvironment garminEnvironment)
	{
		switch (garminEnvironment)
		{
		case GarminEnvironment.Production:
		case GarminEnvironment.Demo:
		case GarminEnvironment.China:
			return "https://ghs.garmin.com/";
		case GarminEnvironment.Stage:
		case GarminEnvironment.Test:
		case GarminEnvironment.ChinaTest:
			return "https://ghs-stg.garmin.com/";
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	async ValueTask IAuthenticator.Authenticate(IRestClient client, RestRequest request)
	{
		if (_unitId != null)
		{
			DIToken dIToken = (await _auth.GetDIAuthAsync(_unitId)) ?? throw new DIAuthorizationException();
			request.AddHeader("Authorization", "Bearer " + dIToken.AccessToken);
		}
	}
}
