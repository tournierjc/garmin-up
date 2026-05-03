using System.Threading.Tasks;
using RestSharp;

namespace Networking.Connect;

[NetworkingService(typeof(IUserService))]
internal class UserService : IUserService
{
	private readonly ConnectRestClient _restClient;

	public UserService(ConnectRestClient restClient)
	{
		_restClient = restClient;
	}

	public async Task SendTimezoneToConnectAsync(string timeZone)
	{
		RestRequest request = new RestRequest("/user-service-1.0/json/timezone?value=" + timeZone, Method.Post);
		request.AddXmlBody("");
		await _restClient.ExecuteAsync(request);
	}
}
