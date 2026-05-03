using System;
using Microsoft.Extensions.Logging;

namespace Networking.OAuth;

internal sealed class SsoRestClient : NetworkingRestClient
{
	public SsoRestClient(ILogger<SsoRestClient> logger, GarminEnvironment garminEnvironment)
		: base(logger, GetBaseUrl(garminEnvironment))
	{
	}

	public static string GetBaseUrl(GarminEnvironment garminEnvironment)
	{
		switch (garminEnvironment)
		{
		case GarminEnvironment.Production:
		case GarminEnvironment.Demo:
			return "https://sso.garmin.com/";
		case GarminEnvironment.Stage:
			return "https://ssostg.garmin.com/";
		case GarminEnvironment.Test:
			return "https://ssotest.garmin.com/";
		case GarminEnvironment.China:
			return "https://sso.garmin.cn/";
		case GarminEnvironment.ChinaTest:
			return "https://ssotest-china.garmin.com/";
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
