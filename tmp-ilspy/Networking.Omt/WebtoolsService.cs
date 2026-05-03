using System.Net;
using System.Threading.Tasks;
using RestSharp;

namespace Networking.Omt;

internal sealed class WebtoolsService : IWebtoolsService
{
	public async Task<bool> GetIsIntranetAsync()
	{
		return (await new WebtoolsRestClient(GarminEnvironment.Test).ExecuteAsync(new RestRequest(), Method.Head)).StatusCode == HttpStatusCode.OK;
	}
}
