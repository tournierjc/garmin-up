using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;

namespace Networking.Connect;

[NetworkingService(typeof(IActivitySearchService))]
internal class ActivitySearchService : IActivitySearchService
{
	private readonly ConnectRestClient _restClient;

	private readonly UnitId _unitId;

	public ActivitySearchService(ConnectRestClient restClient, UnitId unitId)
	{
		_restClient = restClient;
		_unitId = unitId;
	}

	public async Task<RestResponse> GetActivityMatchesAsync(params string[] ids)
	{
		StringBuilder stringBuilder = new StringBuilder($"deviceId={_unitId.Id}");
		foreach (string item in ids.OrderBy((string i) => i))
		{
			stringBuilder.AppendFormat("&externalIds=" + item);
		}
		RestRequest request = new RestRequest($"activity-search-service-1.2/json/matchesByDeviceId?{stringBuilder}", Method.Post);
		request.AddBody(stringBuilder.ToString(), ContentType.FormUrlEncoded);
		return await _restClient.ExecuteAsync(request);
	}
}
