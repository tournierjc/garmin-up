using Newtonsoft.Json;

namespace Networking.Omt.Dto.UniversalMaps;

public class ActivateUpdatesRequest
{
	[JsonProperty("garminDeviceXml")]
	public string GarminDeviceXml { get; }

	[JsonProperty("installedMaps")]
	public MapInstallIdentifier[] InstalledMaps { get; }

	public ActivateUpdatesRequest(string gdxml, MapInstallIdentifier mapInstallIdentifier)
	{
		GarminDeviceXml = gdxml;
		InstalledMaps = new MapInstallIdentifier[1] { mapInstallIdentifier };
	}

	public ActivateUpdatesRequest(string gdxml, MapInstallIdentifier[] mapInstallIdentifier)
	{
		GarminDeviceXml = gdxml;
		InstalledMaps = mapInstallIdentifier;
	}
}
