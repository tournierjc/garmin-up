using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Networking.InternalOmt;

internal class InternalOmtRestClient : NetworkingRestClient
{
	public InternalOmtRestClient(ILogger<InternalOmtRestClient> logger, GarminEnvironment garminEnvironment)
		: base(logger, GetBaseUrl(garminEnvironment))
	{
		AddDefaultHeader("Garmin-Client-Name", Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName));
	}

	private static string GetBaseUrl(GarminEnvironment garminEnvironment)
	{
		switch (garminEnvironment)
		{
		case GarminEnvironment.Production:
		case GarminEnvironment.Demo:
			return "https://omt-int.garmin.com/";
		case GarminEnvironment.Stage:
			return "https://omtstg-int.garmin.com/";
		case GarminEnvironment.Test:
			return "https://omttest-int.garmin.com/";
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
