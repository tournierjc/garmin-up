using System.Threading.Tasks;
using RestSharp;

namespace Networking.Connect;

public interface IActivitySearchService
{
	Task<RestResponse> GetActivityMatchesAsync(params string[] ids);
}
