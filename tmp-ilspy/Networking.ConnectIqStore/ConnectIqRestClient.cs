using Microsoft.Extensions.Logging;

namespace Networking.ConnectIqStore;

internal sealed class ConnectIqRestClient : NetworkingRestClient
{
	public ConnectIqRestClient(ILogger<ConnectIqRestClient> logger, GarminEnvironment garminEnvironment, ClientId clientId)
		: base(logger, GetBaseUrl(garminEnvironment))
	{
		AddDefaultHeader("x-garmin-client-id", clientId.Id);
	}

	public static string GetBaseUrl(GarminEnvironment garminEnvironment)
	{
		if (garminEnvironment == GarminEnvironment.Test)
		{
			return "https://apps-test.garmin.com/";
		}
		return "https://apps.garmin.com/";
	}
}
