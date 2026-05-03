using System;
using Microsoft.Extensions.Logging;

namespace Networking.Geolocation;

internal sealed class GeolocationRestClient : NetworkingRestClient
{
	public GeolocationRestClient(ILogger<GeolocationRestClient> logger, GarminEnvironment garminEnvironment)
		: base(logger, GetBaseUrl(garminEnvironment))
	{
	}

	public static string GetBaseUrl(GarminEnvironment garminEnvironment)
	{
		switch (garminEnvironment)
		{
		case GarminEnvironment.Production:
		case GarminEnvironment.Demo:
			return "https://geolocation.garmin.com/";
		case GarminEnvironment.Stage:
			return "https://geolocation-stage.garmin.com/";
		case GarminEnvironment.Test:
		case GarminEnvironment.ChinaTest:
			return "https://geolocation-test.garmin.com/";
		case GarminEnvironment.China:
			return "https://geolocation.garmin.cn/";
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
