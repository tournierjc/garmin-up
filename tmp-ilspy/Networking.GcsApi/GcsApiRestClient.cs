using System;
using Microsoft.Extensions.Logging;

namespace Networking.GcsApi;

internal sealed class GcsApiRestClient : NetworkingRestClient
{
	public GcsApiRestClient(ILogger<GcsApiRestClient> logger, GarminEnvironment garminEnvironment)
		: base(logger, GetBaseUrl(garminEnvironment))
	{
	}

	public static string GetBaseUrl(GarminEnvironment garminEnvironment)
	{
		switch (garminEnvironment)
		{
		case GarminEnvironment.Production:
		case GarminEnvironment.Demo:
			return "https://api.gcs.garmin.com/";
		case GarminEnvironment.Stage:
			return "https://api.gcs.stage.garmin.com/";
		case GarminEnvironment.Test:
		case GarminEnvironment.ChinaTest:
			return "https://api.gcs.test.garmin.com/";
		case GarminEnvironment.China:
			return "https://api.gcs.garmin.cn/";
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
