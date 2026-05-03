using System;
using RestSharp;

namespace Networking.Omt;

internal sealed class WebtoolsRestClient : RestClient
{
	public WebtoolsRestClient(GarminEnvironment garminEnvironment)
		: base(GetBaseUrl(garminEnvironment))
	{
	}

	internal static string GetBaseUrl(GarminEnvironment garminEnvironment)
	{
		switch (garminEnvironment)
		{
		case GarminEnvironment.Production:
			return "https://webtools.garmin.com/";
		case GarminEnvironment.Stage:
			return "https://webtools-stg.garmin.com/";
		case GarminEnvironment.Test:
		case GarminEnvironment.ChinaTest:
			return "https://webtools-test.garmin.com/";
		case GarminEnvironment.China:
			return "https://webtools.cn1.garmin.com/";
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
