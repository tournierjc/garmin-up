using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Authenticators;

namespace Networking.Omt;

internal sealed class OmtJwtRestClient : OmtRestClient, IAuthenticator
{
	private readonly IAuthDataProvider _auth;

	private readonly CustomerGuid? _customerGuid;

	public OmtJwtRestClient(ILogger<OmtJwtRestClient> logger, GarminEnvironment garminEnvironment, SessionGuid sessionGuid, AppVersion appVersion, IAuthDataProvider auth, CustomerGuid? customerGuid = null)
		: base(logger, garminEnvironment, sessionGuid, appVersion)
	{
		_auth = auth;
		_customerGuid = customerGuid;
	}

	async ValueTask IAuthenticator.Authenticate(IRestClient client, RestRequest request)
	{
		if (_customerGuid != null)
		{
			await new JwtAuthenticator(((await _auth.GetOmtAuthAsync(_customerGuid)) ?? throw new OmtAuthorizationException()).EncodedToken).Authenticate(client, request);
		}
	}
}
