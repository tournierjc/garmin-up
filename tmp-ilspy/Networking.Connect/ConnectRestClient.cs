using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Authenticators;

namespace Networking.Connect;

internal sealed class ConnectRestClient : NetworkingRestClient, IAuthenticator
{
	private readonly IAuthDataProvider _auth;

	private readonly UnitId? _unitId;

	public ConnectRestClient(ILogger<ConnectRestClient> logger, GarminEnvironment garminEnvironment, IAuthDataProvider auth, ClientId clientId, SessionGuid sessionGuid, AppVersion appVersion, UnitId? unitId = null)
		: base(logger, GetBaseUrl(garminEnvironment))
	{
		_auth = auth;
		_unitId = unitId;
		AddDefaultHeader("Garmin-Client-Name", Constants.AppName);
		AddDefaultHeader("Garmin-Client-Version", appVersion.Version.ToString());
		AddDefaultHeader("Garmin-Client-Platform", Constants.OSPlatform.ToString());
		AddDefaultHeader("Garmin-Client-Platform-Version", Environment.OSVersion.Version.ToString());
		AddDefaultHeader("Garmin-Client-SessionId", sessionGuid.Guid.ToString());
		AddDefaultHeader("X-garmin-client-id", clientId.Id);
	}

	public static string GetBaseUrl(GarminEnvironment garminEnvironment)
	{
		switch (garminEnvironment)
		{
		case GarminEnvironment.Production:
		case GarminEnvironment.Demo:
			return "https://connectapi.garmin.com/";
		case GarminEnvironment.Stage:
			return "https://connectapistg.garmin.com/";
		case GarminEnvironment.Test:
		case GarminEnvironment.ChinaTest:
			return "https://connectapitest.garmin.com/";
		case GarminEnvironment.China:
			return "https://connectapi.garmin.cn/";
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	async ValueTask IAuthenticator.Authenticate(IRestClient client, RestRequest request)
	{
		if (_unitId != null)
		{
			await ConnectTokenProvider.GetAuthenticatorForAccessToken(_auth.GetConnectAuth(_unitId) ?? throw new ConnectAuthorizationException()).Authenticate(client, request);
		}
	}
}
