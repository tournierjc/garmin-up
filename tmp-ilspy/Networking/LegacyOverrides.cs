using Networking.Connect;
using Networking.ITServices;
using Networking.OAuth;
using Networking.Omt;

namespace Networking;

public static class LegacyOverrides
{
	public const string StaticCdnBaseUrl = "https://static.garmincdn.com/";

	public static GarminEnvironment GarminEnvironment { get; internal set; }

	public static string OmtBaseUrl => OmtRestClient.GetBaseUrl(GarminEnvironment);

	public static string SyncBaseUrl => ConnectRestClient.GetBaseUrl(GarminEnvironment);

	public static string ItServicesBaseUrl => ITRestClient.GetBaseUrl(GarminEnvironment);

	public static string SsoBaseUrl => SsoRestClient.GetBaseUrl(GarminEnvironment);

	public static string ConnectBaseUrl
	{
		get
		{
			switch (GarminEnvironment)
			{
			case GarminEnvironment.Stage:
				return "https://connectstg.garmin.com/";
			case GarminEnvironment.Test:
			case GarminEnvironment.ChinaTest:
				return "https://connecttest.garmin.com/";
			case GarminEnvironment.China:
				return "https://connect.garmin.cn/";
			default:
				return "https://connect.garmin.com/";
			}
		}
	}

	public static string ConnectIqStoreBaseUrl
	{
		get
		{
			switch (GarminEnvironment)
			{
			case GarminEnvironment.Test:
			case GarminEnvironment.ChinaTest:
				return "https://apps-test.garmin.com/";
			case GarminEnvironment.China:
				return "https://apps.garmin.cn/";
			default:
				return "https://apps.garmin.com/";
			}
		}
	}

	public static string WhiteLabelCheckoutBaseUrl
	{
		get
		{
			switch (GarminEnvironment)
			{
			case GarminEnvironment.Stage:
				return "https://buygarminstg.garmin.com/";
			case GarminEnvironment.Test:
			case GarminEnvironment.ChinaTest:
				return "https://buygarmintest.garmin.com/";
			default:
				return "https://buy.garmin.com/";
			}
		}
	}

	public static string GarminBaseUrl
	{
		get
		{
			switch (GarminEnvironment)
			{
			case GarminEnvironment.Test:
			case GarminEnvironment.ChinaTest:
				return "https://www.dev.garmin.com/";
			case GarminEnvironment.China:
				return "https://www.garmin.cn/";
			default:
				return "https://www.garmin.com/";
			}
		}
	}

	public static string MyGarminBaseUrl
	{
		get
		{
			switch (GarminEnvironment)
			{
			case GarminEnvironment.Stage:
				return "https://mygarminstg.garmin.com/";
			case GarminEnvironment.Test:
			case GarminEnvironment.ChinaTest:
				return "https://mygarmintest.garmin.com/";
			default:
				return "https://my.garmin.com/";
			}
		}
	}
}
