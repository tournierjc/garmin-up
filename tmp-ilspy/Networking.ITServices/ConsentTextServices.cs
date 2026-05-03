using System.Threading.Tasks;
using Networking.ITServices.Dto;
using RestSharp;
using RestSharp.Authenticators;

namespace Networking.ITServices;

internal class ConsentTextServices : IConsentTextServices
{
	private class EmptyAuthenticator : IAuthenticator
	{
		public ValueTask Authenticate(IRestClient client, RestRequest request)
		{
			return default(ValueTask);
		}
	}

	private readonly ITRestClient _restClient;

	public ConsentTextServices(ITRestClient restClient)
	{
		_restClient = restClient;
	}

	public async Task<ConsentContent[]> GetContentAsync(string consentType, string locale)
	{
		RestRequest restRequest = new RestRequest("/consentTextServices/consentText?consentTypeId=" + consentType + "&locale=" + locale);
		restRequest.Authenticator = new EmptyAuthenticator();
		return (await _restClient.ExecuteAsync<ConsentContent[]>(restRequest)).Data;
	}
}
