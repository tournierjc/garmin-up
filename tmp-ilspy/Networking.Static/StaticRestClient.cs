using Microsoft.Extensions.Logging;

namespace Networking.Static;

internal sealed class StaticRestClient : NetworkingRestClient
{
	public StaticRestClient(ILogger<StaticRestClient> logger)
		: base(logger, "https://static.garmin.com/")
	{
	}
}
