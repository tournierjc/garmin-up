using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Networking.Auth;
using RestSharp;
using RestSharp.Authenticators;

namespace Networking.Omt;

internal sealed class OmtITRestClient : OmtRestClient, IAuthenticator
{
	private readonly IAuthDataProvider _auth;

	private readonly UnitId? _unitId;

	private readonly CustomerGuid? _customerGuid;

	public OmtITRestClient(ILogger<OmtITRestClient> logger, GarminEnvironment garminEnvironment, SessionGuid sessionGuid, AppVersion appVersion, IAuthDataProvider auth, UnitId? unitId = null, CustomerGuid? customerGuid = null)
		: base(logger, garminEnvironment, sessionGuid, appVersion)
	{
		_auth = auth;
		_unitId = unitId;
		_customerGuid = customerGuid;
	}

	async ValueTask IAuthenticator.Authenticate(IRestClient client, RestRequest request)
	{
		ITAuth iTAuth = null;
		if (_customerGuid != null)
		{
			iTAuth = await _auth.GetITAuthAsync(_customerGuid);
		}
		if (iTAuth == null && _unitId != null)
		{
			iTAuth = await _auth.GetITAuthAsync(_unitId);
		}
		if (iTAuth == null)
		{
			throw new ITAuthorizationException();
		}
		request.AddHeader("Authorization", "Bearer " + iTAuth.AccessToken);
	}
}
