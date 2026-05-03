namespace Networking.Omt.Dto.SoftwareUpdateService;

internal class AllUnitSoftwareUpdatesRequest
{
	public string? GarminDeviceXml { get; set; }

	public bool IsUserInteractive { get; set; }

	public object ClientInfo => new object();
}
