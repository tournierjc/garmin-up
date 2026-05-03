using System;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Networking.Omt;

internal class OmtRestClient : NetworkingRestClient
{
	public OmtRestClient(ILogger<OmtRestClient> logger, GarminEnvironment garminEnvironment, SessionGuid sessionGuid, AppVersion appVersion)
		: base(logger, GetBaseUrl(garminEnvironment))
	{
		AddDefaultHeader("Garmin-Client-Name", Constants.AppName);
		AddDefaultHeader("Garmin-Client-Version", appVersion.Version.ToString());
		AddDefaultHeader("Garmin-Client-Platform", Constants.OSPlatform.ToString());
		AddDefaultHeader("Garmin-Client-Platform-Version", Environment.OSVersion.Version.ToString());
		AddDefaultHeader("Garmin-Client-LocaleCode", CultureInfo.CurrentCulture.Name);
		AddDefaultHeader("Garmin-Client-SessionId", sessionGuid.Guid.ToString());
		AddDefaultHeader("Accept-Language", CultureInfo.CurrentCulture.Name);
	}

	public static string GetBaseUrl(GarminEnvironment garminEnvironment)
	{
		switch (garminEnvironment)
		{
		case GarminEnvironment.Production:
			return "https://omt.garmin.com/";
		case GarminEnvironment.Stage:
			return "https://omtstg.garmin.com/";
		case GarminEnvironment.Test:
		case GarminEnvironment.ChinaTest:
			return "https://omttest.garmin.com/";
		case GarminEnvironment.Demo:
			return "https://omtdemo.garmin.com/";
		case GarminEnvironment.China:
			return "https://omt.garmin.cn/";
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
